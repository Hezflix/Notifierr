using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using PlexNotifierr.Core.Services;
using PlexNotifierr.Discord.Services;

namespace PlexNotifierr.Discord.Modules;

public class SubscribeManagementCommands : ModuleBase<ShardedCommandContext>
{
    private readonly ILocalHandler _localHandler;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscribeManagementCommands> _logger;

    public SubscribeManagementCommands(
        ILocalHandler localHandler,
        ISubscriptionService subscriptionService,
        ILogger<SubscribeManagementCommands> logger)
    {
        _localHandler = localHandler;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [Command("subscribe", RunMode = RunMode.Async)]
    public async Task SubscribeWithoutPlexName()
    {
        await Context.Message.ReplyAsync(_localHandler.GetLocales().SubscribeNoPlex);
    }

    [Command("subscribe", RunMode = RunMode.Async)]
    public async Task Subscribe(string plexName)
    {
        _logger.LogInformation("{Username} subscribing as plex user {PlexName}", Context.User.Username, plexName);
        var locales = _localHandler.GetLocales();
        var ok = await _subscriptionService.SubscribeAsync(Context.User.Id.ToString(), plexName);
        await Context.Message.ReplyAsync(ok ? locales.SubscribeSuccess : locales.SubscribeError);
    }

    [Command("unsubscribe", RunMode = RunMode.Async)]
    public async Task Unsubscribe()
    {
        _logger.LogInformation("{Username} unsubscribing", Context.User.Username);
        var locales = _localHandler.GetLocales();
        var ok = await _subscriptionService.UnsubscribeAsync(Context.User.Id.ToString());
        await Context.Message.ReplyAsync(ok ? locales.UnsubscribeSuccess : locales.UnsubscribeError);
    }
}
