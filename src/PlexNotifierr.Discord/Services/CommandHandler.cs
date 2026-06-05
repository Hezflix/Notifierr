using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexNotifierr.Discord.Config;

namespace PlexNotifierr.Discord.Services;

public class CommandHandler : ICommandHandler
{
    private readonly DiscordShardedClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandHandler> _logger;
    private readonly char _prefix;

    public CommandHandler(
        IServiceProvider services,
        DiscordShardedClient client,
        CommandService commands,
        IOptions<DiscordBotConfig> options,
        ILogger<CommandHandler> logger)
    {
        _services = services;
        _client = client;
        _commands = commands;
        _logger = logger;
        _prefix = string.IsNullOrEmpty(options.Value.CommandPrefix) ? '!' : options.Value.CommandPrefix[0];
    }

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        _client.MessageReceived += HandleCommandAsync;
        _client.ShardConnected += OnShardConnectedAsync;

        foreach (var module in _commands.Modules)
        {
            _logger.LogInformation("Module {ModuleName} initialized", module.Name);
        }
    }

    private async Task OnShardConnectedAsync(DiscordSocketClient shard)
    {
        await shard.SetGameAsync($"{_prefix}help", type: ActivityType.Listening);
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        if (arg is not SocketUserMessage msg) return;
        if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

        var context = new ShardedCommandContext(_client, msg);
        var markPos = 0;
        if (msg.HasCharPrefix(_prefix, ref markPos) || msg.HasCharPrefix('?', ref markPos))
        {
            await _commands.ExecuteAsync(context, markPos, _services);
        }
    }
}
