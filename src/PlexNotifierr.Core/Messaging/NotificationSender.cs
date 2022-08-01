using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.ServerApi.PlexModels.Media;
using PlexNotifierr.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PlexNotifierr.Core.Messaging
{
    public class NotificationSender : INotificationSender
    {
        private readonly string _hostName;
        private readonly string _userName;
        private readonly string _password;
        private readonly int _port;
        private readonly string _virtualHost;
        private IConnection? _connection;
        private readonly ILogger _logger;

        public NotificationSender(IOptions<RabbitMqConfig> options, ILogger<NotificationSender> logger)
        {
            _hostName = options.Value.HostName;
            _userName = options.Value.UserName;
            _password = options.Value.Password;
            _port = options.Value.Port;
            _virtualHost = options.Value.VirtualHost;
            _logger = logger;

            CreateConnection();
        }

        public bool TrySendMessage(string discordId, Media media, Metadata episode)
        {
            try
            {
                if (!ConnectionExists()) return false;
                using var channel = _connection!.CreateModel();
                channel.QueueDeclare("discord", true, false, false, null);
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };
                var props = channel.CreateBasicProperties();
                props.ContentType = "application/json; charset=UTF-8";
                var messageJson = JsonSerializer.Serialize(new { discordId, media.Title, episode.Summary, media.ThumbUrl, Season = episode.ParentIndex, Episode = episode.Index, EpisodeTitle = episode.Title, GrandParentRatingKey = episode.GrandparentKey }, options);
                var messageBytes = Encoding.UTF8.GetBytes(messageJson);
                channel.BasicPublish("", "discord", true, props, messageBytes);
                _logger.LogInformation("Successful sent message to discord for user {0} on show {1}", discordId, media.Title);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while publishing message to {discordId} on show {media.Title} {e.Message}");
                return false;
            }
        }

        private void CreateConnection()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _hostName,
                    UserName = _userName,
                    Password = _password,
                    Port = _port,
                    VirtualHost = _virtualHost
                };
                _connection = factory.CreateConnection();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private bool ConnectionExists()
        {
            if (_connection != null) return true;
            CreateConnection();
            return _connection != null;
        }
    }
}