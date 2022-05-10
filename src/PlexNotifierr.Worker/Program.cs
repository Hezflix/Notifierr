using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;


// Create Client Options
var apiOptions = new ClientOptions
{
    Product = "Notifierr",
    DeviceName = "Notifierr",
    ClientId = "MyClientId",
    Platform = "Web",
    Version = "v1"
};

using IHost host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(apiOptions)
            .AddTransient<IPlexServerClient, PlexServerClient>()
            .AddTransient<IPlexAccountClient, PlexAccountClient>()
            .AddTransient<IPlexLibraryClient, PlexLibraryClient>()
            .AddTransient<IApiService, ApiService>()
            .AddTransient<IPlexFactory, PlexFactory>()
            .AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();
    })
    .Build();

host.Run();
