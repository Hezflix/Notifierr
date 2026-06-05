using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Core.Models;
using PlexNotifierr.Core.Services;
using PlexNotifierr.Discord.Extensions;
using Serilog;
using static PlexNotifierr.Api.Extensions.HangfireExtensions;
using static PlexNotifierr.Api.Extensions.PlexExtensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlexNotifierr");

builder.Host.UseSerilog((hostContext, logging) => _ = logging.ReadFrom.Configuration(hostContext.Configuration));

builder.Services.AddDbContextFactory<PlexNotifierrDbContext>(options =>
    options.UseSqlite(
        connectionString,
        x => x.MigrationsAssembly("PlexNotifierr.Core")));
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<PlexNotifierrDbContext>>().CreateDbContext());

builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configFile = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{environment}.json").AddEnvironmentVariables().Build();

builder.Services.AddDiscordBot(configFile);

AddPlexApi(builder.Services, configFile);

AddHangfire(builder.Services, configFile);
AddHangfireServer(builder.Services, configFile);
AddHangfireConsoleExtensions(builder.Services);

var app = builder.Build();

app.UseRouting();

ConfigureHangfireDashboard(app);
ConfigureRecurringJob();

app.Run();
