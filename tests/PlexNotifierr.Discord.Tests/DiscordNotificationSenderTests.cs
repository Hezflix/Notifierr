using Discord;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Plex.ServerApi.PlexModels.Media;
using PlexNotifierr.Discord.Config;
using PlexNotifierr.Discord.Models;
using PlexNotifierr.Discord.Services;
using Shouldly;
using CoreMedia = PlexNotifierr.Core.Models.Media;

namespace PlexNotifierr.Discord.Tests;

public class DiscordNotificationSenderTests
{
    private readonly IDiscordUserGateway _gateway = Substitute.For<IDiscordUserGateway>();
    private readonly ILocalHandler _localHandler = Substitute.For<ILocalHandler>();
    private readonly IDiscordDmRecipient _recipient = Substitute.For<IDiscordDmRecipient>();

    private static readonly CoreMedia Show = new() { Title = "Breaking Bad", ThumbUrl = "http://thumb.jpg" };
    private static readonly Metadata Episode = new()
    {
        Title = "Pilot",
        ParentIndex = 1,
        Index = 1,
        Summary = "Walter starts cooking",
        GrandparentKey = "/library/metadata/100",
    };

    public DiscordNotificationSenderTests()
    {
        _localHandler.GetLocales().Returns(new Locales
        {
            NotificationTitle = "New episode of {Title}: S{Season}E{Episode} - {EpisodeTitle}",
            NotificationCta = "Watch now",
        });
        _recipient.Username.Returns("alice");
    }

    private DiscordNotificationSender CreateSender(
        string plexServerIdentifier = "",
        string plexServerHostName = "")
    {
        var options = Options.Create(new DiscordBotConfig
        {
            PlexServerIdentifier = plexServerIdentifier,
            PlexServerHostName = plexServerHostName,
        });
        return new DiscordNotificationSender(_gateway, _localHandler, options, NullLogger<DiscordNotificationSender>.Instance);
    }

    private static (string Text, Embed Embed) CapturedMessage(IDiscordDmRecipient recipient)
    {
        var call = recipient.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IDiscordDmRecipient.SendMessageAsync));
        var args = call.GetArguments();
        return ((string)args[0]!, (Embed)args[1]!);
    }

    [Fact]
    public async Task TrySendMessageAsync_ReturnsFalse_AndSkipsLookup_WhenDiscordIdNotAUlong()
    {
        var result = await CreateSender().TrySendMessageAsync("not-a-ulong", Show, Episode);

        result.ShouldBeFalse();
        await _gateway.DidNotReceiveWithAnyArgs().GetUserAsync(default);
    }

    [Fact]
    public async Task TrySendMessageAsync_ReturnsFalse_WhenUserNotFound()
    {
        _gateway.GetUserAsync(Arg.Any<ulong>()).Returns((IDiscordDmRecipient?)null);

        var result = await CreateSender().TrySendMessageAsync("123", Show, Episode);

        result.ShouldBeFalse();
        await _recipient.DidNotReceiveWithAnyArgs().SendMessageAsync(default!, default!);
    }

    [Fact]
    public async Task TrySendMessageAsync_SendsTemplatedEmbed_AndReturnsTrue()
    {
        _gateway.GetUserAsync(Arg.Any<ulong>()).Returns(_recipient);

        var result = await CreateSender().TrySendMessageAsync("123", Show, Episode);

        result.ShouldBeTrue();
        var (text, embed) = CapturedMessage(_recipient);
        text.ShouldBe("New episode of Breaking Bad: S1E1 - Pilot");
        embed.Title.ShouldBe("Breaking Bad - Pilot (S1 · E1)");
        embed.Description.ShouldBe("Walter starts cooking");
        embed.Image?.Url.ShouldBe("http://thumb.jpg");
        embed.Color.ShouldBe(Color.DarkPurple);
    }

    [Fact]
    public async Task TrySendMessageAsync_UsesEmptyDescription_WhenSummaryNull()
    {
        _gateway.GetUserAsync(Arg.Any<ulong>()).Returns(_recipient);
        var episode = new Metadata { Title = "Pilot", ParentIndex = 1, Index = 1, Summary = null };

        await CreateSender().TrySendMessageAsync("123", Show, episode);

        var (_, embed) = CapturedMessage(_recipient);
        embed.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task TrySendMessageAsync_AddsPlexLinkField_WhenServerConfigured()
    {
        _gateway.GetUserAsync(Arg.Any<ulong>()).Returns(_recipient);
        var sender = CreateSender(plexServerIdentifier: "abc123", plexServerHostName: "https://plex.example");

        await sender.TrySendMessageAsync("123", Show, Episode);

        var (_, embed) = CapturedMessage(_recipient);
        var field = embed.Fields.ShouldHaveSingleItem();
        field.Name.ShouldBe("View on Plex");
        field.Value.ShouldBe("[Watch now](https://plex.example/web/index.html#!/server/abc123/details?key=/library/metadata/100)");
    }

    [Fact]
    public async Task TrySendMessageAsync_OmitsPlexLinkField_WhenServerNotConfigured()
    {
        _gateway.GetUserAsync(Arg.Any<ulong>()).Returns(_recipient);

        await CreateSender().TrySendMessageAsync("123", Show, Episode);

        var (_, embed) = CapturedMessage(_recipient);
        embed.Fields.ShouldBeEmpty();
    }

    [Fact]
    public async Task TrySendMessageAsync_ReturnsFalse_WhenSendThrows()
    {
        _gateway.GetUserAsync(Arg.Any<ulong>()).Returns(_recipient);
        _recipient.SendMessageAsync(Arg.Any<string>(), Arg.Any<Embed>())
                  .Returns(Task.FromException(new InvalidOperationException("dm closed")));

        var result = await CreateSender().TrySendMessageAsync("123", Show, Episode);

        result.ShouldBeFalse();
    }
}
