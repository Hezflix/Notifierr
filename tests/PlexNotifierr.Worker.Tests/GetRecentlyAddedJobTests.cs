using Hangfire.Console.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.Enums;
using Plex.ServerApi.PlexModels.Library;
using Plex.ServerApi.PlexModels.Media;
using PlexNotifierr.Core.Config;
using PlexNotifierr.Core.Messaging;
using PlexNotifierr.Core.Models;
using PlexNotifierr.TestSupport;
using PlexNotifierr.Worker.Jobs;
using Shouldly;

namespace PlexNotifierr.Worker.Tests;

public sealed class GetRecentlyAddedJobTests : IDisposable
{
    private const int ShowRatingKey = 100;
    private static readonly DateTime OldNotified = new(2000, 1, 1);

    private readonly SqliteInMemoryDatabase _db = new();
    private readonly IPlexServerClient _serverClient = Substitute.For<IPlexServerClient>();
    private readonly INotificationSender _sender = Substitute.For<INotificationSender>();
    private readonly IProgressBarFactory _progressBarFactory = Substitute.For<IProgressBarFactory>();

    public void Dispose() => _db.Dispose();

    private GetRecentlyAddedJob CreateJob(PlexNotifierrDbContext context) =>
        new(context, _serverClient, _sender, _progressBarFactory,
            Options.Create(new PlexConfig { ServerUrl = "http://plex", AccessToken = "token" }),
            NullLogger<GetUsersJob>.Instance);

    /// <summary>One "show" library is returned by Plex.</summary>
    private void GivenShowLibrary() =>
        _serverClient.GetLibrariesAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new LibraryContainer
            {
                Libraries = [new Library { Key = "1", Type = "show", Title = "TV Shows" }],
            });

    /// <summary>The recently-added query returns a single episode for the given grandparent show.</summary>
    private void GivenRecentlyAddedEpisode(string grandparentRatingKey, string originallyAvailableAt, DateTime addedAt) =>
        _serverClient.GetLibraryRecentlyAddedAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchType>(),
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new MediaContainer
            {
                Media =
                [
                    new Metadata
                    {
                        GrandparentRatingKey = grandparentRatingKey,
                        OriginallyAvailableAt = originallyAvailableAt,
                        AddedAt = (long)(addedAt - DateTime.UnixEpoch).TotalSeconds,
                        Title = "S01E05",
                    },
                ],
            });

    private void SeedSubscribedShow(DateTime lastNotified, bool userActive = true, string? discordId = "discord-1")
    {
        _db.Seed(context =>
        {
            var user = new User { PlexName = "alice", PlexId = 5, Active = userActive, DiscordId = discordId };
            var media = new Media { RatingKey = ShowRatingKey, Title = "My Show", LastNotified = lastNotified };
            context.Add(user);
            context.Add(media);
            context.Add(new UserSubscription { User = user, Media = media });
        });
    }

    private DateTime ReloadLastNotified()
    {
        using var context = _db.CreateContext();
        return context.Medias.Single(m => m.RatingKey == ShowRatingKey).LastNotified;
    }

    [Fact]
    public async Task ExecuteAsync_NotifiesActiveSubscriber_AndAdvancesLastNotified()
    {
        SeedSubscribedShow(lastNotified: OldNotified);
        GivenShowLibrary();
        GivenRecentlyAddedEpisode(ShowRatingKey.ToString(), "2026-05-20", new DateTime(2026, 2, 1));
        _sender.TrySendMessageAsync(Arg.Any<string>(), Arg.Any<Media>(), Arg.Any<Metadata>(), Arg.Any<CancellationToken>())
               .Returns(true);

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await _sender.Received(1).TrySendMessageAsync(
            "discord-1",
            Arg.Is<Media>(m => m.RatingKey == ShowRatingKey),
            Arg.Is<Metadata>(e => e.GrandparentRatingKey == ShowRatingKey.ToString()),
            Arg.Any<CancellationToken>());
        ReloadLastNotified().ShouldBeGreaterThan(new DateTime(2025, 1, 1));
    }

    [Fact]
    public async Task ExecuteAsync_SkipsShow_WhenAlreadyNotifiedAfterEpisodeDate()
    {
        SeedSubscribedShow(lastNotified: new DateTime(2030, 1, 1));
        GivenShowLibrary();
        GivenRecentlyAddedEpisode(ShowRatingKey.ToString(), "2026-05-20", new DateTime(2026, 2, 1));

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await _sender.DidNotReceive().TrySendMessageAsync(
            Arg.Any<string>(), Arg.Any<Media>(), Arg.Any<Metadata>(), Arg.Any<CancellationToken>());
        ReloadLastNotified().ShouldBe(new DateTime(2030, 1, 1));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotAdvanceLastNotified_WhenSendFails()
    {
        SeedSubscribedShow(lastNotified: OldNotified);
        GivenShowLibrary();
        GivenRecentlyAddedEpisode(ShowRatingKey.ToString(), "2026-05-20", new DateTime(2026, 2, 1));
        _sender.TrySendMessageAsync(Arg.Any<string>(), Arg.Any<Media>(), Arg.Any<Metadata>(), Arg.Any<CancellationToken>())
               .Returns(false);

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await _sender.Received(1).TrySendMessageAsync(
            Arg.Any<string>(), Arg.Any<Media>(), Arg.Any<Metadata>(), Arg.Any<CancellationToken>());
        ReloadLastNotified().ShouldBe(OldNotified);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotNotify_InactiveSubscriber()
    {
        SeedSubscribedShow(lastNotified: OldNotified, userActive: false);
        GivenShowLibrary();
        GivenRecentlyAddedEpisode(ShowRatingKey.ToString(), "2026-05-20", new DateTime(2026, 2, 1));

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await _sender.DidNotReceive().TrySendMessageAsync(
            Arg.Any<string>(), Arg.Any<Media>(), Arg.Any<Metadata>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresNonShowLibraries()
    {
        _serverClient.GetLibrariesAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new LibraryContainer
            {
                Libraries = [new Library { Key = "2", Type = "movie", Title = "Movies" }],
            });

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await _serverClient.DidNotReceive().GetLibraryRecentlyAddedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchType>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }
}
