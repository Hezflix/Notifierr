using PlexNotifierr.DiscordBot.Models;
using PlexNotifierr.DiscordBot.Services.Interfaces;
using System.Text.Json;

namespace PlexNotifierr.DiscordBot.Services;

public class LocalHandler : ILocalHandler
{
    private Locales? Locales { get; set; }
    private const string fileName = "locales.json";

    private void InitializeAsync()
    {
        using var fileStream = File.OpenRead(fileName);
        Locales = JsonSerializer.Deserialize<Locales>(fileStream)!;
    }

    public Locales GetLocales()
    {
        if (Locales is null)
        {
            InitializeAsync();
        }
        return Locales!;
    }
}