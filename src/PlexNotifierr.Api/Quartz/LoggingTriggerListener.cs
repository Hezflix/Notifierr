using Microsoft.Extensions.Logging;
using Quartz;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PlexNotifierr.Api.Quartz
{
    public class LoggingTriggerListener : ITriggerListener
    {
        public LoggingTriggerListener(ILogger<LoggingTriggerListener> logger)
        {
            Logger = logger;
        }

        private ILogger Logger { get; }

        public string Name { get; set; } = "Logging Trigger History Plugin";

        /// <inheritdoc/>
        public Task TriggerFired(
            ITrigger trigger,
            IJobExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            Logger.LogInformation($"Le trigger {{Nom}} a déclenché le job {{JobName}}", trigger.Key.ToString(), trigger.JobKey.ToString());
            var stopwatch = new Stopwatch();
            stopwatch.Restart();
            context.Put(nameof(Stopwatch), stopwatch);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task TriggerMisfired(
            ITrigger trigger,
            CancellationToken cancellationToken = default)
        {
            Logger.LogInformation(
                $"Le trigger {{Nom}} a manqué d'exécuter le job {{JobName}}. L'exécution aurait dû être déclenchée à : {{NextFireTime}}",
                trigger.Key.ToString(),
                trigger.JobKey.ToString(),
                trigger.GetNextFireTimeUtc());
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task TriggerComplete(
            ITrigger trigger,
            IJobExecutionContext context,
            SchedulerInstruction triggerInstructionCode,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = context.Get(nameof(Stopwatch)) as Stopwatch;
            stopwatch?.Stop();

            Logger.LogInformation(
                $"Le trigger {{Nom}} a terminé l'exécution du job {{JobName}} en {{ElapsedMilliseconds}} ms avec le code d'instruction suivant : {{TriggerInstructionCode}}",
                trigger.Key.ToString(),
                trigger.JobKey.ToString(),
                stopwatch?.ElapsedMilliseconds,
                triggerInstructionCode.ToString());
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> VetoJobExecution(
            ITrigger trigger,
            IJobExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}