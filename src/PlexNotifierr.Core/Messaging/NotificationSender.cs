using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.ServerApi.PlexModels.Media;
using PlexNotifierr.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PlexNotifierr.Core.Messaging;

public sealed class NotificationSender : INotificationSender, IAsyncDisposable
{
    private readonly RabbitMqConfig _config;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public NotificationSender(IOptions<RabbitMqConfig> options, ILogger<NotificationSender> logger)
    {
        _config = options.Value;
        _logger = logger;
    }

    public async Task<bool> TrySendMessageAsync(string discordId, Media media, Metadata episode, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await EnsureConnectionAsync(cancellationToken);
            if (connection is null) return false;

            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync("discord", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

            var serializerOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            var props = new BasicProperties
            {
                ContentType = "application/json; charset=UTF-8"
            };
            var messageJson = JsonSerializer.Serialize(new { discordId, media.Title, episode.Summary, media.ThumbUrl, Season = episode.ParentIndex, Episode = episode.Index, EpisodeTitle = episode.Title, GrandParentRatingKey = episode.GrandparentKey }, serializerOptions);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            await channel.BasicPublishAsync(exchange: "", routingKey: "discord", mandatory: true, basicProperties: props, body: messageBytes, cancellationToken: cancellationToken);
            _logger.LogDebug("Message sent successfully to RBMQ for user {DiscordId} on show {MediaTitle}", discordId, media.Title);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while publishing message to {DiscordId} on show {MediaTitle}", discordId, media.Title);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        _connectionLock.Dispose();
    }

    private async Task<IConnection?> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                UserName = _config.UserName,
                Password = _config.Password,
                Port = _config.Port,
                VirtualHost = _config.VirtualHost
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to open RabbitMQ connection to {HostName}:{Port}", _config.HostName, _config.Port);
            return null;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
