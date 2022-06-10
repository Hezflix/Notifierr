using PlexNotifierr.Core.Messaging;

namespace PlexNotifierr.Api.Extensions
{
    public static class PublicationExtensions
    {
        public static void AddRabbitMqSender(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMqConfig>().Bind(configuration.GetSection("RabbitMQ"));
            services.AddSingleton<INotificationSender, NotificationSender>();
        }
    }
}