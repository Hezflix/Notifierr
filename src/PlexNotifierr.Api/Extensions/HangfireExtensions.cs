using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.Storage.SQLite;
using PlexNotifierr.Api.Hangfire;
using PlexNotifierr.Worker.Jobs;

namespace PlexNotifierr.Api.Extensions
{
    public class HangfireExtensions
    {
        public static void AddHangfire(IServiceCollection services, IConfiguration configuration)
        {
            var connectionStringHangfire = configuration.GetConnectionString("Hangfire");
            services.AddHangfire(options =>
            {
                options.UseSQLiteStorage(connectionStringHangfire)
                       .WithJobExpirationTimeout(TimeSpan.FromDays(30))
                       .UseDashboardMetric(DashboardMetrics.ServerCount)
                       .UseDashboardMetric(DashboardMetrics.RecurringJobCount)
                       .UseDashboardMetric(DashboardMetrics.RetriesCount)
                       .UseDashboardMetric(DashboardMetrics.EnqueuedCountOrNull)
                       .UseDashboardMetric(DashboardMetrics.FailedCountOrNull)
                       .UseDashboardMetric(DashboardMetrics.EnqueuedAndQueueCount)
                       .UseDashboardMetric(DashboardMetrics.ScheduledCount)
                       .UseDashboardMetric(DashboardMetrics.ProcessingCount)
                       .UseDashboardMetric(DashboardMetrics.SucceededCount)
                       .UseDashboardMetric(DashboardMetrics.FailedCount)
                       .UseDashboardMetric(DashboardMetrics.DeletedCount)
                       .UseDashboardMetric(DashboardMetrics.AwaitingCount)
                       .UseHeartbeatPage(TimeSpan.FromSeconds(5))
                       .UseConsole();
            });
        }

        public static void AddHangfireServer(IServiceCollection services, IConfiguration configuration)
        {
            var connectionStringHangfire = configuration.GetConnectionString("Hangfire");
            services.AddHangfireServer((_, options) =>
            {
                options.Queues = new[] { "default" };
                options.ServerName = "WORKER-" + Environment.MachineName;
                options.WorkerCount = 1;
            }, new SQLiteStorage(connectionStringHangfire), new[] { new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(5)) });
        }

        public static void AddHangfireConsoleExtensions(IServiceCollection services)
        {
            services.AddHangfireConsoleExtensions();
        }

        public static void ConfigureHangfireDashboard(WebApplication app)
        {
            app.UseHangfireDashboard("/dashboard",options: new DashboardOptions
            {
                Authorization = new[] { new NoAuthorizationFilter() },
                DashboardTitle = "Notifierr Dashboard",
                DisplayStorageConnectionString = true,
                AppPath = null
            });
        }

        public static void ConfigureRecurringJob()
        {
            RecurringJob.AddOrUpdate<GetUsersJob>(x => x.ExecuteAsync(), Cron.Daily);
            RecurringJob.AddOrUpdate<GetUsersHistoryJob>(x => x.ExecuteAsync(), Cron.Hourly);
            RecurringJob.AddOrUpdate<GetRecentlyAddedJob>(x => x.ExecuteAsync(), Cron.Minutely);
        }
    }
}