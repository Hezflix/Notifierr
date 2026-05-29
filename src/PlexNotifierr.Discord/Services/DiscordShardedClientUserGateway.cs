using Discord;
using Discord.WebSocket;

namespace PlexNotifierr.Discord.Services;

/// <summary>
/// Production <see cref="IDiscordUserGateway"/> backed by the live <see cref="DiscordShardedClient"/>.
/// This is the thin, untested adapter layer: it only forwards to Discord.Net so all testable logic
/// can live in <see cref="DiscordNotificationSender"/>.
/// </summary>
internal sealed class DiscordShardedClientUserGateway : IDiscordUserGateway
{
    private readonly DiscordShardedClient _client;

    public DiscordShardedClientUserGateway(DiscordShardedClient client) => _client = client;

    public async Task<IDiscordDmRecipient?> GetUserAsync(ulong userId)
    {
        var user = await _client.Rest.GetUserAsync(userId);
        return user is null ? null : new RestUserRecipient(user);
    }

    private sealed class RestUserRecipient : IDiscordDmRecipient
    {
        private readonly IUser _user;

        public RestUserRecipient(IUser user) => _user = user;

        public string Username => _user.Username;

        public Task SendMessageAsync(string text, Embed embed) => _user.SendMessageAsync(text, embed: embed);
    }
}
