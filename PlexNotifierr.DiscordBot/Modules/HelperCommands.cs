using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using PlexNotifierr.DiscordBot.Services.Interfaces;

namespace PlexNotifierr.DiscordBot.Modules;

public class HelperCommands : ModuleBase<ShardedCommandContext>
{
    private readonly ILocalHandler _localHandler;
    private readonly ILogger<HelperCommands> _logger;

    public HelperCommands(ILocalHandler localHandler, ILogger<HelperCommands> logger)
    {
        _localHandler = localHandler;
        _logger = logger;
    }

    [Command("help", RunMode = RunMode.Async)]
    public async Task? Help()
    {
        _logger.LogInformation("Help requested by {UserUsername}", Context.User.Username);
        var locales = _localHandler.GetLocales();
        var embedBuilder = new EmbedBuilder()
                          .WithAuthor(Context.Client.CurrentUser.Username, Context.Client.CurrentUser.GetAvatarUrl())
                          .WithTitle(locales.HelpTitle)
                          .AddField("\u200b", "\u200b")
                          .AddField("!subscribe {username Plex}", locales.SubscribeHelp)
                          .AddField("!unsubscribe", locales.UnsubscribeHelp)
                          .WithColor(Color.DarkPurple)
                          .WithCurrentTimestamp();

        await Context.Message.ReplyAsync(embed: embedBuilder.Build());
    }
}