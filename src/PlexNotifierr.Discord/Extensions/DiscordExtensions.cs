using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlexNotifierr.Core.Messaging;
using PlexNotifierr.Discord.Config;
using PlexNotifierr.Discord.Services;

namespace PlexNotifierr.Discord.Extensions;

public static class DiscordExtensions
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DiscordBotConfig>().Bind(configuration.GetSection("Discord"));

        services.AddSingleton(_ => new DiscordShardedClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 1000,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
        }));

        services.AddSingleton(_ => new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false,
        }));

        services.AddSingleton<ILocalHandler, LocalHandler>();
        services.AddSingleton<ICommandHandler, CommandHandler>();
        services.AddSingleton<INotificationSender, DiscordNotificationSender>();
        services.AddHostedService<DiscordBotHostedService>();

        return services;
    }
}
