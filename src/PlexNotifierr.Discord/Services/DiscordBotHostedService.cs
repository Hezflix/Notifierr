using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexNotifierr.Discord.Config;
using PlexNotifierr.Discord.Extensions;

namespace PlexNotifierr.Discord.Services;

public class DiscordBotHostedService : IHostedService
{
    private readonly DiscordShardedClient _client;
    private readonly ICommandHandler _commandHandler;
    private readonly ILogger<DiscordBotHostedService> _logger;
    private readonly string _token;
    private bool _started;

    public DiscordBotHostedService(
        DiscordShardedClient client,
        ICommandHandler commandHandler,
        IOptions<DiscordBotConfig> options,
        ILogger<DiscordBotHostedService> logger)
    {
        _client = client;
        _commandHandler = commandHandler;
        _logger = logger;
        _token = options.Value.BotToken;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            _logger.LogWarning("Discord bot token is not configured; the Discord bot will not start.");
            return;
        }

        _client.Log += message => DiscordLoggerExtensions.LogAsync(_logger, message);
        _client.ShardReady += shard =>
        {
            _logger.LogInformation("Shard {ShardId} connected and ready", shard.ShardId);
            return Task.CompletedTask;
        };

        await _commandHandler.InitializeAsync();
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        _started = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started) return;
        await _client.LogoutAsync();
        await _client.StopAsync();
    }
}
