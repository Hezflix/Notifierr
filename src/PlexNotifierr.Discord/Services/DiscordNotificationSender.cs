using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.ServerApi.PlexModels.Media;
using PlexNotifierr.Core.Messaging;
using PlexNotifierr.Core.Models;
using PlexNotifierr.Discord.Config;

namespace PlexNotifierr.Discord.Services;

public class DiscordNotificationSender : INotificationSender
{
    private readonly DiscordShardedClient _client;
    private readonly ILocalHandler _localHandler;
    private readonly ILogger<DiscordNotificationSender> _logger;
    private readonly string _plexServerIdentifier;
    private readonly string _plexServerHostName;

    public DiscordNotificationSender(
        DiscordShardedClient client,
        ILocalHandler localHandler,
        IOptions<DiscordBotConfig> options,
        ILogger<DiscordNotificationSender> logger)
    {
        _client = client;
        _localHandler = localHandler;
        _logger = logger;
        _plexServerIdentifier = options.Value.PlexServerIdentifier;
        _plexServerHostName = options.Value.PlexServerHostName;
    }

    public async Task<bool> TrySendMessageAsync(string discordId, Media media, Metadata episode, CancellationToken cancellationToken = default)
    {
        if (!ulong.TryParse(discordId, out var userId))
        {
            _logger.LogWarning("DiscordId {DiscordId} is not a valid ulong; skipping", discordId);
            return false;
        }

        try
        {
            var user = await _client.Rest.GetUserAsync(userId);
            if (user is null)
            {
                _logger.LogWarning("Discord user {DiscordId} not found", discordId);
                return false;
            }

            var locales = _localHandler.GetLocales();
            var title = locales.NotificationTitle
                .Replace("{Title}", media.Title)
                .Replace("{Season}", episode.ParentIndex.ToString())
                .Replace("{Episode}", episode.Index.ToString())
                .Replace("{EpisodeTitle}", episode.Title);

            var embedBuilder = new EmbedBuilder()
                .WithTitle($"{media.Title} - {episode.Title} (S{episode.ParentIndex} · E{episode.Index})")
                .WithDescription(episode.Summary ?? string.Empty)
                .WithImageUrl(media.ThumbUrl)
                .WithColor(Color.DarkPurple)
                .WithCurrentTimestamp();

            if (!string.IsNullOrWhiteSpace(_plexServerIdentifier) && !string.IsNullOrWhiteSpace(_plexServerHostName))
            {
                var link = $"{_plexServerHostName}/web/index.html#!/server/{_plexServerIdentifier}/details?key={episode.GrandparentKey}";
                embedBuilder.AddField("View on Plex", $"[{locales.NotificationCta}]({link})");
            }

            await user.SendMessageAsync(title, embed: embedBuilder.Build());
            _logger.LogInformation("Notification sent to {Username} for {Title}", user.Username, media.Title);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send Discord notification to {DiscordId} for {Title}", discordId, media.Title);
            return false;
        }
    }
}
