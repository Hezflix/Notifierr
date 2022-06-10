using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;
using PlexNotifierr.Core.Config;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PlexNotifierr.Api.Extensions
{
    public static class PlexExtensions
    {
        public static void AddPlexApi(IServiceCollection services, IConfigurationRoot configuration)
        {
            _ = services.Configure<PlexConfig>(configuration.GetSection("Plex"));
            var plexConfig = configuration.GetSection("Plex").Get<PlexConfig>();
            var apiOptions = new ClientOptions
            {
                Product = plexConfig.Product,
                DeviceName = plexConfig.DeviceName,
                ClientId = plexConfig.ClientId,
                Platform = RuntimeInformation.OSDescription,
                Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()?.Version ?? "v0"
            };
            services.AddSingleton(apiOptions)
                    .AddTransient<IPlexServerClient, PlexServerClient>()
                    .AddTransient<IPlexAccountClient, PlexAccountClient>()
                    .AddTransient<IPlexLibraryClient, PlexLibraryClient>()
                    .AddTransient<IApiService, ApiService>()
                    .AddTransient<IPlexFactory, PlexFactory>()
                    .AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();
        }
    }
}