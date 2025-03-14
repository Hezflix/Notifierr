using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Api.Extensions;
using PlexNotifierr.Core.Context;
using PlexNotifierr.DiscordBot.Services;
using PlexNotifierr.DiscordBot.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlexNotifierr");

var client = new DiscordShardedClient(new DiscordSocketConfig
{
    LogLevel = LogSeverity.Info,
    AlwaysDownloadUsers = true,
    MessageCacheSize = 1000
});

var commands = new CommandService(new CommandServiceConfig
{
    // Again, log level:
    LogLevel = LogSeverity.Info,

    // There's a few more properties you can set,
    // for example, case-insensitive commands.
    CaseSensitiveCommands = false
});

builder.Services.AddSingleton(client);
builder.Services.AddSingleton(commands);
builder.Services.AddSingleton<ICommandHandler, CommandHandler>();
builder.Services.AddSingleton<ILocalHandler, LocalHandler>();

// Add services to the container.
builder.Host.UseSerilog((hostContext, logging) => _ = logging.ReadFrom.Configuration(hostContext.Configuration));

builder.Services.AddControllers();
builder.Services.AddDbContext<PlexNotifierrDbContext>(options =>
    options.UseSqlite(
        connectionString,
        x => x.MigrationsAssembly("PlexNotifierr.Core"))
);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configFile = new ConfigurationBuilder().AddJsonFile($"appsettings.json").AddJsonFile($"appsettings.{environment}.json").AddEnvironmentVariables().Build();

// AddRabbitMqSender(builder.Services, configFile);

builder.Services.AddPlexApi(configFile);

builder.Services.AddHangfire(configFile);
builder.Services.AddHangfireServer(configFile);
builder.Services.AddHangfireConsoleExtension();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.ConfigureHangfireDashboard();
app.ConfigureRecurringJob();
client.ShardReady += async shard => { Log.Information($"Shard Number {shard.ShardId} is connected and ready!"); };
var token = configFile.GetRequiredSection("Discord")["DiscordBotToken"];
await app.Services.GetService<ICommandHandler>()?.InitializeAsync()!;
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();
app.Run();