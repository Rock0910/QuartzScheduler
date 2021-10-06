#region Listner說明
//嘗試做串聯任務的東西，讓他能夠兩個兩個綁看看
#endregion

using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzTests.Listeners
{
    
    public class JobChainingJobListener : JobListenerSupport
    {
        private readonly Dictionary<JobKey, JobKey> chainLinks;

        /// <summary>
        /// Construct an instance with the given name.
        /// </summary>
        /// <param name="name">The name of this instance.</param>
        public JobChainingJobListener(string name)
        {
            Name = name ?? throw new ArgumentException("Listener name cannot be null!");
            chainLinks = new Dictionary<JobKey, JobKey>();
        }

        public string Name { get; }

        /// <summary>
        /// Add a chain mapping - when the Job identified by the first key completes
        /// the job identified by the second key will be triggered.
        /// </summary>
        /// <param name="firstJob">a JobKey with the name and group of the first job</param>
        /// <param name="secondJob">a JobKey with the name and group of the follow-up job</param>
        public void AddJobChainLink(JobKey firstJob, JobKey secondJob)
        {
            if (firstJob == null || secondJob == null)
            {
                throw new ArgumentException("Key cannot be null!");
            }
            if (firstJob.Name == null || secondJob.Name == null)
            {
                throw new ArgumentException("Key cannot have a null name!");
            }

            chainLinks.Add(firstJob, secondJob);
        }

        public async Task JobWasExecuted(IJobExecutionContext context,
            JobExecutionException? jobException,
            CancellationToken cancellationToken = default)
        {
            chainLinks.TryGetValue(context.JobDetail.Key, out var sj);

            if (sj == null)
            {
                return;
            }


            try
            {
                await context.Scheduler.TriggerJob(sj, cancellationToken).ConfigureAwait(false);
            }
            catch (SchedulerException se)
            {
            }
        }
    }
}
