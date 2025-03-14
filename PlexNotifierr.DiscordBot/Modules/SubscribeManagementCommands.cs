using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlexNotifierr.Core.Context;
using PlexNotifierr.DiscordBot.Services.Interfaces;

namespace PlexNotifierr.DiscordBot.Modules;

public class SubscribeManagementCommands : ModuleBase<ShardedCommandContext>
{
    private readonly ILocalHandler _localHandler;
    private ILogger<SubscribeManagementCommands> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;


    public SubscribeManagementCommands(ILocalHandler localHandler, IServiceScopeFactory serviceScopeFactory, ILogger<SubscribeManagementCommands> logger)
    {
        _localHandler = localHandler;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    [Command("subscribe", RunMode = RunMode.Async)]
    public async Task? SubscribeWithoutPlexName()
    {
        await Context.Message.ReplyAsync(_localHandler.GetLocales().SubscribeNoPlex);
    }

    [Command("subscribe", RunMode = RunMode.Async)]
    public async Task Subscribe(string plexName)
    {
        _logger.LogInformation("User {UserUsername} subscribe on plex user {PlexName}", Context.User.Username, plexName);
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlexNotifierrDbContext>();
        var userId = Context.User.Id.ToString();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.DiscordId == userId);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(plexName))
            {
                await Context.Message.ReplyAsync(_localHandler.GetLocales().SubscribeError);
                return;
            }
            user = dbContext.Users.FirstOrDefault(x => x.PlexName == plexName);
            if (user is null)
            {
                await Context.Message.ReplyAsync(_localHandler.GetLocales().SubscribeError);
                return;
            }
            user.DiscordId = userId;
        }
        user.Active = true;
        await dbContext.SaveChangesAsync();
        await Context.Message.ReplyAsync(_localHandler.GetLocales().SubscribeSuccess);
    }

    [Command("unsubscribe", RunMode = RunMode.Async)]
    public async Task Unsubscribe()
    {
        _logger.LogInformation("User {UserUsername} unsubscribe", Context.User.Username);
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlexNotifierrDbContext>();
        var userId = Context.User.Id.ToString();
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.DiscordId == userId);
        if (user is null)
        {
            await Context.Message.ReplyAsync(_localHandler.GetLocales().UnsubscribeError);
            return;
        }
        user.Active = false;
        await dbContext.SaveChangesAsync();
        await Context.Message.ReplyAsync(_localHandler.GetLocales().UnsubscribeSuccess);
    }
}