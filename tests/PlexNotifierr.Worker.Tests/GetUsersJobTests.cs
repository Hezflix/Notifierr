using Hangfire.Console.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Plex.Api.Factories;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Account;
using PlexAccount = Plex.Library.ApiModels.Accounts.PlexAccount;
using PlexNotifierr.Core.Config;
using PlexNotifierr.Core.Models;
using PlexNotifierr.TestSupport;
using PlexNotifierr.Worker.Jobs;
using Shouldly;

namespace PlexNotifierr.Worker.Tests;

public sealed class GetUsersJobTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _db = new();
    private readonly IPlexFactory _plexFactory = Substitute.For<IPlexFactory>();
    private readonly IPlexAccountClient _accountClient = Substitute.For<IPlexAccountClient>();
    private readonly IProgressBarFactory _progressBarFactory = Substitute.For<IProgressBarFactory>();

    public void Dispose() => _db.Dispose();

    /// <summary>
    /// <see cref="GetUsersJob"/> takes the concrete <see cref="PlexAccount"/> (whose members are
    /// non-virtual, so it cannot be substituted directly). Instead we build a real PlexAccount over
    /// substituted Plex clients and stub the underlying GetFriendsAsync call that Friends() delegates to.
    /// </summary>
    private void GivenPlexAccount(string ownerUsername, params Friend[] friends)
    {
        var account = new PlexAccount(
            _accountClient,
            Substitute.For<IPlexServerClient>(),
            Substitute.For<IPlexLibraryClient>(),
            authToken: "token")
        {
            Username = ownerUsername,
        };
        _accountClient.GetFriendsAsync(Arg.Any<string>()).Returns(friends.ToList());
        _plexFactory.GetPlexAccount(Arg.Any<string>()).Returns(account);
    }

    private GetUsersJob CreateJob(PlexNotifierrDbContext context) =>
        new(context, _plexFactory, _progressBarFactory,
            Options.Create(new PlexConfig { ServerUrl = "http://plex", AccessToken = "token" }),
            NullLogger<GetUsersJob>.Instance);

    private List<User> AllUsers()
    {
        using var context = _db.CreateContext();
        return context.Users.AsNoTracking().ToList();
    }

    [Fact]
    public async Task ExecuteAsync_AddsOwnerAndFriends_WhenDatabaseEmpty()
    {
        GivenPlexAccount("owner",
            new Friend { Id = 5, Username = "alice" },
            new Friend { Id = 6, Username = "bob" });

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        var users = AllUsers();
        users.Count.ShouldBe(3);
        users.ShouldContain(u => u.PlexId == 1 && u.PlexName == "owner");
        users.ShouldContain(u => u.PlexId == 5 && u.PlexName == "alice");
        users.ShouldContain(u => u.PlexId == 6 && u.PlexName == "bob");
        users.ShouldAllBe(u => !u.Active); // newly imported users start inactive
    }

    [Fact]
    public async Task ExecuteAsync_RenamesExistingUser_WithoutTouchingActiveFlag()
    {
        _db.Seed(c =>
        {
            c.Users.Add(new User { PlexId = 1, PlexName = "owner", Active = false });
            c.Users.Add(new User { PlexId = 5, PlexName = "old-name", Active = true });
        });
        GivenPlexAccount("owner", new Friend { Id = 5, Username = "new-name" });

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        var renamed = AllUsers().Single(u => u.PlexId == 5);
        renamed.PlexName.ShouldBe("new-name");
        renamed.Active.ShouldBeTrue(); // sync only updates the name, never the subscription state
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotDuplicateOwner_WhenOwnerAlreadyExists()
    {
        _db.Seed(c => c.Users.Add(new User { PlexId = 1, PlexName = "owner", Active = false }));
        GivenPlexAccount("owner"); // no friends

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        AllUsers().Count(u => u.PlexId == 1).ShouldBe(1);
    }
}
