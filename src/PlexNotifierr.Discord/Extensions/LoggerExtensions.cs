using Discord;
using Microsoft.Extensions.Logging;

namespace PlexNotifierr.Discord.Extensions;

public static class DiscordLoggerExtensions
{
    public static Task LogAsync(ILogger logger, LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information,
        };
        logger.Log(level, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
