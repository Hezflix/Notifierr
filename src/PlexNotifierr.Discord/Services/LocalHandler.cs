using System.Text.Json;
using Microsoft.Extensions.Options;
using PlexNotifierr.Discord.Config;
using PlexNotifierr.Discord.Models;

namespace PlexNotifierr.Discord.Services;

public class LocalHandler : ILocalHandler
{
    private readonly string _path;
    private Locales? _locales;

    public LocalHandler(IOptions<DiscordBotConfig> options)
    {
        _path = options.Value.LocalesPath;
    }

    public Locales GetLocales()
    {
        if (_locales is not null) return _locales;

        using var stream = File.OpenRead(_path);
        _locales = JsonSerializer.Deserialize<Locales>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? new Locales();
        return _locales;
    }
}
