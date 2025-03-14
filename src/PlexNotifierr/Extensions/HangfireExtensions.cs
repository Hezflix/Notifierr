using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.Storage.SQLite;
using PlexNotifierr.Api.Hangfire;
using PlexNotifierr.Worker.Jobs;

namespace PlexNotifierr.Api.Extensions;

public static class HangfireExtensions
{
    public static void AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStringHangfire = configuration.GetConnectionString("Hangfire");
        services.AddHangfire(options =>
        {
            options.UseSQLiteStorage(connectionStringHangfire)
                   .UseHeartbeatPage(TimeSpan.FromSeconds(5))
                   .UseConsole();
        });
    }

    public static void AddHangfireServer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStringHangfire = configuration.GetConnectionString("Hangfire");
        services.AddHangfireServer((_, options) =>
        {
            options.Queues = ["default"];
            options.ServerName = "WORKER-" + Environment.MachineName;
            options.WorkerCount = 1;
        }, new SQLiteStorage(connectionStringHangfire), new[] { new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(5)) });
    }

    public static void AddHangfireConsoleExtension(this IServiceCollection services) => services.AddHangfireConsoleExtensions();

    public static void ConfigureHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/dashboard",options: new DashboardOptions
        {
            Authorization = new[] { new NoAuthorizationFilter() },
            DashboardTitle = "Notifierr Dashboard",
            DisplayStorageConnectionString = true,
            AppPath = null
        });
    }

    public static void ConfigureRecurringJob(this WebApplication app)
    {
        RecurringJob.AddOrUpdate<GetUsersJob>("GetUsers", x => x.ExecuteAsync(), Cron.Daily);
        RecurringJob.AddOrUpdate<GetUsersHistoryJob>("GetUsersHistory", x => x.ExecuteAsync(), Cron.Hourly);
        RecurringJob.AddOrUpdate<GetRecentlyAddedJob>("GetRecentlyAdded", x => x.ExecuteAsync(), Cron.Hourly);
    }
}