
using Microsoft.EntityFrameworkCore;
using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;
using PlexNotifierr.Api;
using PlexNotifierr.Core.Models;
using PlexNotifierr.Worker.Jobs;
using Quartz;
using System.Reflection;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlexNotifierr");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<PlexNotifierrDbContext>(options =>
                  options.UseSqlite(
                            connectionString,
                            x => x.MigrationsAssembly("PlexNotifierr.Core"))
                   );

var assembly = Assembly.GetExecutingAssembly();
string? version = assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
var configFile = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
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

builder.Services.AddTransient(sp => new GetUsersJob(sp.GetRequiredService<PlexNotifierrDbContext>(), sp.GetRequiredService<IPlexFactory>().GetPlexAccount(configPlex.AccessToken)));

builder.Services.AddQuartz(q =>
{
    q.SchedulerId = "Scheduler-Core";
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });

    var jobKey = new JobKey("GetUsersJob");

    // Register the job with the DI container
    q.AddJob<GetUsersJob>(opts => opts.WithIdentity(jobKey));

    // Create a trigger for the job
    q.AddTrigger(opts => opts
        .ForJob(jobKey) // link to the Job
        .WithIdentity("GetUsersJob-trigger") // give the trigger a unique name
        .WithCronSchedule("0/55 * * * * ?")); // run every 55 seconds

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
