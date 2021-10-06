using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzTests.Listeners
{
    public class JobListener :IJobListener
    {
        private readonly ILogger<JobListener> _logger;
        private readonly IServiceProvider _serviceProvider = null;
        string IJobListener.Name => "Jobs Listener";

        public JobListener(ILogger<JobListener> logger , IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var jobName = context.JobDetail.Key.Name;
            _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 準備執行工作 | {jobName}");

            //var schedulerHub = _serviceProvider.GetRequiredService<SchedulerHub>();
            //await schedulerHub.NotifyJobStatusChange();

            await Task.CompletedTask;
        }
        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            var jobName = context.JobDetail.Key.Name;
            _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 工作執行完畢 | {jobName}");
            await Task.CompletedTask;
        }

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

    }
}
