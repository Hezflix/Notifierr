namespace PlexNotifierr.Discord.Config;

public class DiscordBotConfig
{
    public string BotToken { get; set; } = string.Empty;

    public string PlexServerIdentifier { get; set; } = string.Empty;

    public string PlexServerHostName { get; set; } = string.Empty;

    public string CommandPrefix { get; set; } = "!";

    public string LocalesPath { get; set; } = "locales.json";
}
