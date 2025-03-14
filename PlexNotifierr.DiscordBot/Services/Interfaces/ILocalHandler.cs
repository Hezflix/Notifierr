using PlexNotifierr.DiscordBot.Models;

namespace PlexNotifierr.DiscordBot.Services.Interfaces;

public interface ILocalHandler
{
    Locales GetLocales();
}