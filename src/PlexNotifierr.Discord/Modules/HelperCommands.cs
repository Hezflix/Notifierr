using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexNotifierr.Discord.Config;
using PlexNotifierr.Discord.Services;

namespace PlexNotifierr.Discord.Modules;

public class HelperCommands : ModuleBase<ShardedCommandContext>
{
    private readonly ILocalHandler _localHandler;
    private readonly ILogger<HelperCommands> _logger;
    private readonly string _prefix;

    public HelperCommands(ILocalHandler localHandler, IOptions<DiscordBotConfig> options, ILogger<HelperCommands> logger)
    {
        _localHandler = localHandler;
        _logger = logger;
        _prefix = string.IsNullOrEmpty(options.Value.CommandPrefix) ? "!" : options.Value.CommandPrefix;
    }

    [Command("help", RunMode = RunMode.Async)]
    public async Task Help()
    {
        _logger.LogInformation("Help requested by {Username}", Context.User.Username);
        var locales = _localHandler.GetLocales();
        var embed = new EmbedBuilder()
            .WithAuthor(Context.Client.CurrentUser.Username, Context.Client.CurrentUser.GetAvatarUrl())
            .WithTitle(locales.HelpTitle)
            .AddField("​", "​")
            .AddField($"{_prefix}subscribe {{plex username}}", locales.SubscribeHelp)
            .AddField($"{_prefix}unsubscribe", locales.UnsubscribeHelp)
            .WithColor(Color.DarkPurple)
            .WithCurrentTimestamp()
            .Build();

        await Context.Message.ReplyAsync(embed: embed);
    }
}
