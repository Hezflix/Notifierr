using Discord;

namespace PlexNotifierr.Discord.Services;

/// <summary>
/// Thin seam over Discord.Net's user lookup and direct-messaging. It exists so the notification
/// logic in <see cref="DiscordNotificationSender"/> can be unit-tested without a live gateway
/// connection — Discord.Net's <c>DiscordShardedClient</c> and <c>RestUser</c> are concrete types
/// with non-virtual members and cannot be substituted directly. The production implementation is
/// <see cref="DiscordShardedClientUserGateway"/>.
/// </summary>
public interface IDiscordUserGateway
{
    /// <summary>Resolves a Discord user by id, or <c>null</c> if no such user exists.</summary>
    Task<IDiscordDmRecipient?> GetUserAsync(ulong userId);
}

/// <summary>A Discord user that can receive a direct message.</summary>
public interface IDiscordDmRecipient
{
    string Username { get; }

    Task SendMessageAsync(string text, Embed embed);
}
