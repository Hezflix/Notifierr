using Microsoft.EntityFrameworkCore;
using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;
using PlexNotifierr.Api;
using PlexNotifierr.Core.Messaging;
using PlexNotifierr.Core.Models;
using PlexNotifierr.Worker.Jobs;
using Quartz;
using Serilog;
using System.Reflection;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlexNotifierr");

// Add services to the container.
builder.Host.UseSerilog((hostContext, logging) => _ = logging.ReadFrom.Configuration(hostContext.Configuration));

builder.Services.AddControllers();
builder.Services.AddDbContext<PlexNotifierrDbContext>(options =>
    options.UseSqlite(
        connectionString,
        x => x.MigrationsAssembly("PlexNotifierr.Core"))
);

var assembly = Assembly.GetExecutingAssembly();
var version = assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
var configFile = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();

builder.Services.AddOptions<RabbitMqConfig>().Bind(configFile.GetSection("RabbitMQ"));
builder.Services.AddSingleton<INotificationSender, NotificationSender>();

var configPlex = new PlexConfig();
configFile.Bind("Plex", configPlex);
// Create Client Options
var apiOptions = new ClientOptions
{
    Product = configPlex.Product,
    DeviceName = configPlex.DeviceName,
    ClientId = configPlex.ClientId,
    Platform = RuntimeInformation.OSDescription,
    Version = version ?? "v0"
};

builder.Services.AddSingleton(apiOptions)
       .AddTransient<IPlexServerClient, PlexServerClient>()
       .AddTransient<IPlexAccountClient, PlexAccountClient>()
       .AddTransient<IPlexLibraryClient, PlexLibraryClient>()
       .AddTransient<IApiService, ApiService>()
       .AddTransient<IPlexFactory, PlexFactory>()
       .AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();

builder.Services.Configure<QuartzOptions>(options =>
{
    options.Scheduling.IgnoreDuplicates = true;
    options.Scheduling.OverWriteExistingData = true;
});

builder.Services.AddTransient(sp => new GetUsersJob(sp.GetRequiredService<PlexNotifierrDbContext>(), sp.GetRequiredService<IPlexFactory>().GetPlexAccount(configPlex.AccessToken), sp.GetRequiredService<ILogger<GetUsersJob>>()));
builder.Services.AddTransient(sp => new GetUsersHistoryJob(sp.GetRequiredService<PlexNotifierrDbContext>(), sp.GetRequiredService<IPlexServerClient>(), configPlex.ServerUrl, configPlex.AccessToken, sp.GetRequiredService<ILogger<GetUsersHistoryJob>>()));
builder.Services.AddTransient(sp => new GetRecentlyAddedJob(sp.GetRequiredService<PlexNotifierrDbContext>(), sp.GetRequiredService<IPlexServerClient>(), sp.GetRequiredService<INotificationSender>(), configPlex.ServerUrl, configPlex.AccessToken));

builder.Services.AddQuartz(q =>
{
    q.SchedulerId = "Scheduler-Core";
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

    // var getUsersJobKey = new JobKey("GetUsersJob");
    // var getUsersHistoryJobKey = new JobKey("GetUsersHistoryJob");
    var getRecentlyAddedJobKey = new JobKey("GetRecentlyAddedJob");

    // Register the job with the DI container
    // q.AddJob<GetUsersJob>(opts => opts.WithIdentity(getUsersJobKey));
    // q.AddJob<GetUsersHistoryJob>(opts => opts.WithIdentity(getUsersHistoryJobKey));
    q.AddJob<GetRecentlyAddedJob>(opts => opts.WithIdentity(getRecentlyAddedJobKey));

    // Create a trigger for the job
    // q.AddTrigger(opts => opts
    //                     .ForJob(getUsersJobKey) // link to the Job
    //                     .WithIdentity("GetUsersJob-trigger")
    //                     .StartNow()); // give the trigger a unique name
    //    .WithCronSchedule("* 10 * * * ?")); // run every 55 seconds

    // q.AddTrigger(opts => opts
    //     .ForJob(getUsersHistoryJobKey) // link to the Job
    //     .WithIdentity("GetUsersHistoryJob-trigger")
    //     .StartNow());// give the trigger a unique name
    //.WithCronSchedule("* 10 * * * ?")); // run every 55 seconds

    q.AddTrigger(opts => opts
                        .ForJob(getRecentlyAddedJobKey) // link to the Job
                        .WithIdentity("GetRecentlyAddedJob-trigger")
                        .StartNow());
}).AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddQuartzHostedService(options =>
{
    // when shutting down we want jobs to complete gracefully
    options.WaitForJobsToComplete = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();