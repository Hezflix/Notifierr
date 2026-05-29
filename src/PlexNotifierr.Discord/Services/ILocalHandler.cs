using PlexNotifierr.Discord.Models;

namespace PlexNotifierr.Discord.Services;

public interface ILocalHandler
{
    Locales GetLocales();
}
