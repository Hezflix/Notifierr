using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PlexNotifierr.DiscordBot.Services.Interfaces;
using Serilog;
using System.Reflection;

namespace PlexNotifierr.DiscordBot.Services;

public class CommandHandler : ICommandHandler
{
    private readonly DiscordShardedClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;

    public CommandHandler(IServiceProvider services,
                          DiscordShardedClient client,
                          CommandService commands)
    {
        _services = services;
        _client = client;
        _commands = commands;
    }

    public async Task InitializeAsync()
    {
        // add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        // Subscribe a handler to see if a message invokes a command.
        _client.MessageReceived += HandleCommandAsync;

        _client.ShardConnected += Connected;
        //await _client.SetActivityAsync(IActivity);
        foreach (var module in _commands.Modules) Log.Information("Module \'{ModuleName}\' initialized", module.Name);
    }

    private async Task Connected(DiscordSocketClient discordSocketClient)
    {
        await discordSocketClient.SetGameAsync("!help", type: ActivityType.Listening);
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        // Bail out if it's a System Message.
        if (arg is not SocketUserMessage msg)
            return;

        // We don't want the bot to respond to itself or other bots.
        if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot)
            return;

        // Create a Command Context.
        var context = new ShardedCommandContext(_client, msg);

        var markPos = 0;
        if (msg.HasCharPrefix('!', ref markPos) || msg.HasCharPrefix('?', ref markPos))
        {
            await _commands.ExecuteAsync(context, markPos, _services);
        }
    }
}