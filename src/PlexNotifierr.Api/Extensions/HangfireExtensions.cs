using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.Storage.SQLite;
using Microsoft.Data.Sqlite;
using PlexNotifierr.Api.Hangfire;
using PlexNotifierr.Worker.Jobs;
using Serilog;

namespace PlexNotifierr.Api.Extensions;

public class HangfireExtensions
{
    public static void AddHangfire(IServiceCollection services, IConfiguration configuration)
    {
        var connectionStringHangfire = configuration.GetConnectionString("Hangfire");
        EnsureHangfireStorageIsHealthy(connectionStringHangfire);
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

    // The Hangfire.Storage.SQLite database occasionally corrupts ("database disk image is malformed"),
    // which leaks an endless stream of errors from the distributed-lock cleanup loop. Hangfire jobs are
    // transient state (queues + recurring schedules), so on corruption we drop the file and let Hangfire
    // recreate the schema on startup rather than ship with a broken store.
    private static void EnsureHangfireStorageIsHealthy(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var dbPath = ResolveSqliteFilePath(connectionString);
        if (dbPath is null || !File.Exists(dbPath)) return;

        if (IsSqliteDatabaseHealthy(dbPath)) return;

        Log.Warning("Hangfire SQLite database at {DbPath} is corrupt; resetting it", dbPath);
        DeleteSqliteDatabaseFiles(dbPath);
    }

    private static string? ResolveSqliteFilePath(string connectionString)
    {
        // Hangfire.Storage.SQLite accepts either a raw file path or a Microsoft.Data.Sqlite-style
        // connection string ("Data Source=...").
        if (!connectionString.Contains('=', StringComparison.Ordinal))
            return Path.GetFullPath(connectionString);

        try
        {
            return Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSqliteDatabaseHealthy(string dbPath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            using var reader = command.ExecuteReader();
            return reader.Read() && string.Equals(reader.GetString(0), "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Hangfire SQLite integrity check failed for {DbPath}", dbPath);
            return false;
        }
    }

    private static void DeleteSqliteDatabaseFiles(string dbPath)
    {
        foreach (var suffix in new[] { "", "-shm", "-wal", "-journal" })
        {
            var path = dbPath + suffix;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to delete Hangfire SQLite artifact {Path}", path);
            }
        }
    }
}
