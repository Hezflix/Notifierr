namespace PlexNotifierr.Core.Services;

public interface ISubscriptionService
{
    Task<bool> SubscribeAsync(string discordId, string? plexName, CancellationToken cancellationToken = default);

    Task<bool> UnsubscribeAsync(string discordId, CancellationToken cancellationToken = default);
}
