using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Core.Models;
using PlexNotifierr.Core.Services;
using PlexNotifierr.TestSupport;
using Shouldly;

namespace PlexNotifierr.Core.Tests;

public sealed class SubscriptionServiceTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _db = new();
    private readonly SubscriptionService _sut;

    public SubscriptionServiceTests() => _sut = new SubscriptionService(_db);

    public void Dispose() => _db.Dispose();

    private async Task<User?> GetUserByDiscordIdAsync(string discordId)
    {
        await using var context = _db.CreateContext();
        return await context.Users.SingleOrDefaultAsync(u => u.DiscordId == discordId);
    }

    [Fact]
    public async Task SubscribeAsync_ActivatesExistingUser_WhenFoundByDiscordId()
    {
        _db.Seed(c => c.Users.Add(new User { DiscordId = "discord-1", PlexName = "alice", Active = false }));

        var result = await _sut.SubscribeAsync("discord-1", plexName: null);

        result.ShouldBeTrue();
        var user = await GetUserByDiscordIdAsync("discord-1");
        user.ShouldNotBeNull();
        user.Active.ShouldBeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_IgnoresPlexName_WhenUserAlreadyLinkedByDiscordId()
    {
        // A user already linked to this Discord id keeps its original Plex identity even if a
        // different plexName is passed.
        _db.Seed(c => c.Users.Add(new User { DiscordId = "discord-1", PlexName = "alice", Active = false }));

        var result = await _sut.SubscribeAsync("discord-1", plexName: "someone-else");

        result.ShouldBeTrue();
        var user = await GetUserByDiscordIdAsync("discord-1");
        user!.PlexName.ShouldBe("alice");
        user.Active.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SubscribeAsync_ReturnsFalse_WhenUnknownDiscordIdAndNoPlexName(string? plexName)
    {
        var result = await _sut.SubscribeAsync("unknown-discord", plexName);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsFalse_WhenPlexNameDoesNotMatchAnyUser()
    {
        _db.Seed(c => c.Users.Add(new User { PlexName = "alice", Active = false }));

        var result = await _sut.SubscribeAsync("discord-1", plexName: "nobody");

        result.ShouldBeFalse();
        // The unrelated user must remain untouched.
        await using var context = _db.CreateContext();
        var alice = await context.Users.SingleAsync(u => u.PlexName == "alice");
        alice.Active.ShouldBeFalse();
        alice.DiscordId.ShouldBeNull();
    }

    [Fact]
    public async Task SubscribeAsync_LinksDiscordIdAndActivates_WhenPlexNameMatches()
    {
        _db.Seed(c => c.Users.Add(new User { PlexName = "alice", Active = false, DiscordId = null }));

        var result = await _sut.SubscribeAsync("discord-1", plexName: "alice");

        result.ShouldBeTrue();
        await using var context = _db.CreateContext();
        var alice = await context.Users.SingleAsync(u => u.PlexName == "alice");
        alice.DiscordId.ShouldBe("discord-1");
        alice.Active.ShouldBeTrue();
    }

    [Fact]
    public async Task UnsubscribeAsync_DeactivatesUser_WhenFoundByDiscordId()
    {
        _db.Seed(c => c.Users.Add(new User { DiscordId = "discord-1", PlexName = "alice", Active = true }));

        var result = await _sut.UnsubscribeAsync("discord-1");

        result.ShouldBeTrue();
        var user = await GetUserByDiscordIdAsync("discord-1");
        user!.Active.ShouldBeFalse();
    }

    [Fact]
    public async Task UnsubscribeAsync_ReturnsFalse_WhenDiscordIdUnknown()
    {
        var result = await _sut.UnsubscribeAsync("unknown-discord");

        result.ShouldBeFalse();
    }
}
