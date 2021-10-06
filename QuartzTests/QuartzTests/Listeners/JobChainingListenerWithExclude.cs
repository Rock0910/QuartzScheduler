using Quartz.Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz.Logging;
using Quartz;
using System.Threading;

namespace QuartzTests.Listeners
{
    public class JobChainingListenerWithExclude : JobListenerSupport
    {
        private readonly Dictionary<JobKey, JobKey> chainLinks;

        /// <summary>
        /// Construct an instance with the given name.
        /// </summary>
        /// <param name="name">The name of this instance.</param>
        public JobChainingListenerWithExclude(string name)
        {
            Name = name ?? throw new ArgumentException("Listener name cannot be null!");
            chainLinks = new Dictionary<JobKey, JobKey>();
        }

        public override string Name { get; }

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

        public override async Task JobWasExecuted(IJobExecutionContext context,
            JobExecutionException? jobException,
            CancellationToken cancellationToken = default)
        {
            //取得Listener所在的排程器
            var curJobScheduler = context.Scheduler;
            //取得已完成任務名稱
            string finishedJobName = context.JobDetail.Key.Name;
            
            //找Dictionary中要連接的任務名
            chainLinks.TryGetValue(context.JobDetail.Key, out var sj);

            //如果沒有的話那就不做事
            if (sj == null)
            {
                return;
            }

            Console.WriteLine($"Job '{context.JobDetail.Key}' will now try to chain to Job '{sj}'");

            try
            {
                //取得接下來要執行的工作的詳細資料
                var nextJobInfo = curJobScheduler.GetJobDetail(sj);
                //取得之前設定的 excludes(要排他的工作群組)
                List<string> excludes = (List<string>)nextJobInfo.Result.JobDataMap["excludes"];
                //看那些群組名稱
                excludes.ForEach(x => Console.WriteLine(x));

                if (CheckJobs(curJobScheduler,excludes,finishedJobName))//如果排程器正在執行工作的群組相衝
                {
                    do
                    {
                        Console.WriteLine("[X] 排程器中有相衝的工作群組，等待三秒後重試\n");
                        await Task.Delay(TimeSpan.FromSeconds(3));//等三秒
                    } while (CheckJobs(curJobScheduler,excludes,finishedJobName));//如果排程器正在執行工作的群組相衝
                }
                //開始下一個工作
                await context.Scheduler.TriggerJob(sj, cancellationToken).ConfigureAwait(false);
                Console.WriteLine("[O] 開始執行工作!");
                //移除本Listener
                curJobScheduler.ListenerManager.RemoveJobListener(Name);
            }
            catch (SchedulerException se)
            {
                Console.WriteLine($"Error encountered during chaining to Job '{sj}'", se);
            }
        }

        /// <summary>
        /// 檢查傳入的 [排程器] 中正在執行的工作的群組有沒有跟排他群組相衝
        /// </summary>
        /// <param name="scheduler"></param>
        /// 該工作所在的排程器
        /// <param name="groupNames"></param>
        /// 排他的群組
        /// <param name="previousJobName"></param>
        /// 剛執行完畢的任務名稱
        /// <param name="targetName"></param>
        /// 接下來要執行的任務名稱(尚未使用)
        /// <returns></returns>
        private static bool CheckJobs(IScheduler scheduler, List<string> groupNames,string previousJobName ,string targetName = null)
        {
            //正在執行的工作名稱[]
            List<string> jobNames = new List<string>();
            //正在執行的工作群組[]
            List<string> jobGroups = new List<string>();

            //看排程器中的正在執行工作
            foreach (var x in scheduler.GetCurrentlyExecutingJobs().Result)
            {
                //如果找到的資料不是剛執行完的任務才增加其資料到List中
                if (x.JobDetail.Key.Name != previousJobName)
                {
                    //增加該任務名字到陣列中
                    jobNames.Add(x.JobDetail.Key.Name);
                    //如果任務群組不在陣列中
                    if (!jobGroups.Contains(x.JobDetail.Key.Group))
                    {
                        //新增他到任務群組陣列中
                        jobGroups.Add(x.JobDetail.Key.Group);
                    }
                }
                
            }

            Console.WriteLine("正在執行的任務名稱");
            foreach (var item in jobNames)
            {
                Console.Write(item+ " | ");
            }
            Console.WriteLine();


            Console.WriteLine("正在執行工作的群組們");
            foreach (var item in jobGroups)
            {
                Console.Write(item+ " | ");
            }
            Console.WriteLine();
            
            if (jobGroups.Intersect(groupNames).Any()) //取交集，有東西的話就是有衝突
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
