namespace PlexNotifierr.Core.Messaging
{
    public class RabbitMqConfig
    {
        public string HostName { get; set; } = String.Empty;

        public string UserName { get; set; } = String.Empty;

        public string Password { get; set; } = String.Empty;

        public int Port { get; set; }

        public string VirtualHost { get; set; } = String.Empty;
    }
}
