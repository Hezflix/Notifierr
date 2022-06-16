using PlexNotifierr.Api.Quartz;
using PlexNotifierr.Worker.Jobs;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using SilkierQuartz;

namespace PlexNotifierr.Api.Extensions
{
    public static class QuartzExtensions
    {
        public static void AddQuartz(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<QuartzOptions>(options =>
            {
                options.Scheduling.IgnoreDuplicates = true;
                options.Scheduling.OverWriteExistingData = true;
            });
            _ = services
               .AddQuartz(q =>
                {
                    q.UseMicrosoftDependencyInjectionJobFactory();
                    q.UseSimpleTypeLoader();
                    q.UseInMemoryStore();
                    q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });
                })
               .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        }

        public static void RegisterJobs(IServiceCollection services)
        {
            services.AddQuartzJob<GetUsersJob>("GetUsersJob");
            services.AddQuartzJob<GetUsersHistoryJob>("GetUsersHistoryJob");
            services.AddQuartzJob<GetRecentlyAddedJob>("GetRecentlyAddedJob");
        }

        public static void AddSilkierQuartz(IServiceCollection services)
        {
            services.AddSilkierQuartz(options =>
                {
                    options.VirtualPathRoot = "/dashboard";
                    options.UseLocalTime = true;
                    options.DefaultDateFormat = "yyyy-MM-dd";
                    options.DefaultTimeFormat = "HH:mm:ss";
                    options.Scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
                },
                authenticationOptions => { authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous; },
                options =>
                {
                    options.Add("quartz.plugin.recentHistory.storeType", "Quartz.Plugins.RecentHistory.Impl.InProcExecutionHistoryStore, Quartz.Plugins.RecentHistory");
                    options.Add("quartz.plugin.recentHistory.type", "Quartz.Plugins.RecentHistory.ExecutionHistoryPlugin, Quartz.Plugins.RecentHistory");
                }
            );
        }

        public static void ConfigureSilkierQuartzTriggers(WebApplication app)
        {
            StdSchedulerFactory.GetDefaultScheduler()
                               .Result
                               .ListenerManager
                               .AddTriggerListener(
                                    new LoggingTriggerListener(app.Services
                                                                  .GetRequiredService<
                                                                       ILogger<LoggingTriggerListener>>()),
                                    GroupMatcher<TriggerKey>.AnyGroup());

            app.UseQuartzJob<GetUsersJob>(TriggerBuilder.Create()
                                                        .WithIdentity("DailyTrigger")
                                                        .StartNow()
                                                        .WithSimpleSchedule(x => x.WithIntervalInHours(24).RepeatForever())
                                                        .WithDescription("Daily trigger for GetUsersJob"));

            app.UseQuartzJob<GetUsersHistoryJob>(TriggerBuilder.Create()
                                                               .WithIdentity("HourlyTrigger")
                                                               .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddMinutes(5)))
                                                               .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
                                                               .WithDescription("Hourly trigger for GetUsersHistoryJob"));

            app.UseQuartzJob<GetRecentlyAddedJob>(TriggerBuilder.Create()
                                                                .WithIdentity("5MinuteTrigger")
                                                                .StartNow()
                                                                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
                                                                .WithDescription("5 minutes trigger for GetRecentlyAddedJob"));
        }
    }
}
