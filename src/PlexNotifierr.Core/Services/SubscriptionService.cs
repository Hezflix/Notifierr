using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Core.Models;

namespace PlexNotifierr.Core.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IDbContextFactory<PlexNotifierrDbContext> _dbContextFactory;

    public SubscriptionService(IDbContextFactory<PlexNotifierrDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> SubscribeAsync(string discordId, string? plexName, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId, cancellationToken);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(plexName)) return false;
            user = await dbContext.Users.FirstOrDefaultAsync(u => u.PlexName == plexName, cancellationToken);
            if (user is null) return false;
            user.DiscordId = discordId;
        }
        user.Active = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnsubscribeAsync(string discordId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId, cancellationToken);
        if (user is null) return false;
        user.Active = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
