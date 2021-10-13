#region 排程器說明
//正在嘗試進行排他，首先先上了每個Job的群組名稱，打算用這群組名稱來限制另一個群組名稱的等待
#endregion

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;
using Quartz.Spi;
using QuartzTests.DTOs;
using QuartzTests.Jobs;
using QuartzTests.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzTests.Services
{
    public class QuartzHostedService : IHostedService
    {
        //private readonly ISchedulerFactory _schedulerFactory;
        private readonly IScheduler _scheduler;
        private readonly IJobFactory _jobFactory;

        private readonly ILogger<QuartzHostedService> _logger;

        private readonly IEnumerable<JobSchedule> _injectJobSchedules;

        private List<JobSchedule> _allJobSchedules;

        private readonly IJobListener _jobListener;

        //private readonly ISchedulerListener _schedulerListener;

        public IScheduler Scheduler { get; set; }

        public CancellationToken CancellationToken { get; private set; }

        public QuartzHostedService(ILogger<QuartzHostedService> logger, IScheduler scheduler, IJobFactory jobFactory, IEnumerable<JobSchedule> jobSchedules, IJobListener jobListener/*,ISchedulerListener schedulerListener*/)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            _jobListener = jobListener ?? throw new ArgumentNullException(nameof(jobListener));
            //_schedulerListener = schedulerListener ?? throw new ArgumentNullException(nameof(schedulerListener));
            _injectJobSchedules = jobSchedules ?? throw new ArgumentNullException(nameof(jobSchedules));
        }

        /// <summary>
        /// 啟動排程器
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Scheduler == null || Scheduler.IsShutdown)
            {
                // 存下 cancellation token 
                CancellationToken = cancellationToken;

                // 先加入在 startup 註冊注入的 Job 工作
                _allJobSchedules = new List<JobSchedule>();
                _allJobSchedules.AddRange(_injectJobSchedules);

                // 再模擬動態加入新 Job 項目 (e.g. 從 DB 來的，針對不同報表能動態決定產出時機)
                //_allJobSchedules.Add(new JobSchedule(jobName: "0,5*n啟", jobType: typeof(WorkingForLongTime), cronExpression: "0/5 * * * * ?"));


                //_allJobSchedules.Add(new JobSchedule(jobName: "任務編號 1-1", jobGroup: "工作群組 1 ", jobType: typeof(WorkingForLongTime)));
                //_allJobSchedules.Add(new JobSchedule(jobName: "任務編號 1-2", jobGroup: "工作群組 1 ", jobType: typeof(WorkingForLongTime)));
                //_allJobSchedules.Add(new JobSchedule(jobName: "任務編號 1-3", jobGroup: "工作群組 1 ", jobType: typeof(WorkingForLongTime)));


                //_allJobSchedules.Add(new JobSchedule(jobName: "任務編號 2-1", jobGroup: "工作群組 2 ", jobType: typeof(WorkingForLongTime),
                //    excludedGroupNames: new List<string> { "工作群組 1" }
                //    ));
                //_allJobSchedules.Add(new JobSchedule(jobName: "任務編號 2-2", jobGroup: "工作群組 2 ", jobType: typeof(WorkingForLongTime),
                //    excludedGroupNames: new List<string> { "工作群組 1" }
                //    ));
                //_allJobSchedules.Add(new JobSchedule(jobName: "任務編號 2-3", jobGroup: "工作群組 2 ", jobType: typeof(WorkingForLongTime),
                //    excludedGroupNames: new List<string> { "工作群組 1" }
                //    ));


                /*

                ITrigger trigger1 = TriggerBuilder
                .Create()
                .WithIdentity("job1.trigger")
                .WithSimpleSchedule()
                .WithDescription("只執行一次")
                .Build();
                ITrigger trigger2 = TriggerBuilder
                .Create()
                .WithIdentity("job2.trigger")
                .WithSimpleSchedule()
                .WithDescription("只執行一次")
                .Build();
                */

                // 初始排程器 Scheduler
                Scheduler = _scheduler;
                Console.WriteLine(Scheduler.SchedulerName);

                string curJobGroup = "A_1"; //本串任務的群組編號
                List<string> excludeGroupNames = new List<string> { "A_1", "B_1" }; //不能同時執行的任務群組(排他)
                List<KeyValuePair<string, Type>> jobNames = new List<KeyValuePair<string, Type>>(); //子任務串
                jobNames.Add(new KeyValuePair<string, Type>("A1", typeof(WorkingForLongTime)));
                jobNames.Add(new KeyValuePair<string, Type>("A2", typeof(WorkingForLongTime)));
                jobNames.Add(new KeyValuePair<string, Type>("A3", typeof(WorkingForLongTime)));
                jobNames.Add(new KeyValuePair<string, Type>("A4", typeof(WorkingForLongTime)));
                jobNames.Add(new KeyValuePair<string, Type>("A5", typeof(WorkingForLongTime)));

                List<IJobDetail> jobDetails = new List<IJobDetail>();
                foreach (var item in jobNames)
                {
                    IJobDetail curJob = JobBuilder.Create(item.Value)
                        .WithIdentity(item.Key, curJobGroup)
                        .StoreDurably()
                        .Build();
                    curJob.JobDataMap.Put("excludeGroupNames", excludeGroupNames);
                    jobDetails.Add(curJob);
                }
                jobDetails.ForEach(async curJob => await Scheduler.AddJob(curJob, true));

                for (int i = 1; i < jobNames.Count; i++)
                {
                    JobChainingListenerWithExclude curChainer = new JobChainingListenerWithExclude("["+jobNames[i-1].Key+","+curJobGroup+"] → ["+jobNames[i].Key + ","+ curJobGroup +"]");
                    //設定Listener要串哪兩個任務在一起
                    curChainer.AddJobChainLink(jobDetails[i-1].Key, jobDetails[i].Key);
                    //新增這個串聯用Listener到排程器中
                    Scheduler.ListenerManager.AddJobListener(curChainer);
                }

                //測試若撞了是否會停止

                IJobDetail blockerJob = JobBuilder.Create(typeof(WorkingForLongerTime))
                       .WithIdentity("blocker", "B_1")
                       .StoreDurably()
                       .Build();
                blockerJob.JobDataMap.Put("excludeGroupNames", excludeGroupNames);
                await Scheduler.AddJob(blockerJob, true);
                JobChainingListenerWithExclude debugChainer = new JobChainingListenerWithExclude("[" + jobNames[1].Key + "," + curJobGroup + "] → [故意撞的,B_1]");
                //設定Listener要串哪兩個任務在一起
                debugChainer.AddJobChainLink(jobDetails[1].Key, blockerJob.Key);

                //新增這個串聯用Listener到排程器中
                Scheduler.ListenerManager.AddJobListener(debugChainer);


                /*
                //建立兩個要串連的任務
                IJobDetail job1 = JobBuilder.Create<WorkingForLongTime>()
                    .WithIdentity("任務編號 1-1", "工作群組 1")
                    .StoreDurably().Build();
                job1.JobDataMap.Put("excludeGroupNames", excludeGroupNames);

                IJobDetail job2 = JobBuilder.Create<WorkingForLongTime>().WithIdentity("任務編號 2-1", "工作群組 2")
                    .StoreDurably().Build();
                job2.JobDataMap.Put("excludeGroupNames", excludeGroupNames);

                //新增Job進去，記得要讓這個Job StoreDurably() (即使沒在執行也保存)，才能預先放進去
                await Scheduler.AddJob(job1, true);
                await Scheduler.AddJob(job2, true);
                */

                /* 測試沒Durable用的
                IJobDetail job3 = JobBuilder.Create<WorkingForLongTime>()
                    .WithIdentity("任務編號 1-3", "工作群組 1")
                    //.SetJobData(new JobDataMap(extraInfo))
                    .Build();
                job3.JobDataMap.Put("excludes", excludes);
                ITrigger trigger3 = TriggerBuilder
                .Create()
                .WithIdentity("job3.trigger")
                .WithSimpleSchedule()
                .WithDescription("只執行一次")
                .Build();
                await Scheduler.ScheduleJob(job3, trigger3);
                */




                //設定排程器用的產生job用Service
                Scheduler.JobFactory = _jobFactory;
                //設定監聽所有job的監聽器Service到排程器中
                Scheduler.ListenerManager.AddJobListener(_jobListener);

                /*
                //新增一個專門串聯任務用的Listener
                JobChainingListenerWithExclude jobChainingListenerWithExclude = new JobChainingListenerWithExclude("串聯任務用");
                //設定Listener要串哪兩個任務在一起
                jobChainingListenerWithExclude.AddJobChainLink(job1.Key, job2.Key);
                //新增這個串聯用Listener到排程器中
                Scheduler.ListenerManager.AddJobListener(jobChainingListenerWithExclude);

                JobChainingListenerWithExclude jobChainingListenerWithExclude2 = new JobChainingListenerWithExclude("串聯任務用1");
                //設定Listener要串哪兩個任務在一起
                jobChainingListenerWithExclude.AddJobChainLink(job2.Key, job1.Key);

                //新增這個串聯用Listener到排程器中
                Scheduler.ListenerManager.AddJobListener(jobChainingListenerWithExclude2);
                */

                Console.WriteLine("數量" + Scheduler.ListenerManager.GetJobListeners().Count);
                foreach (var item in Scheduler.ListenerManager.GetJobListeners().ToList())
                {
                    Console.WriteLine("Name~~~" + item.Name);
                }
                //啟動第一個Job
                //await Scheduler.TriggerJob(jobDetails[0].Key);

                ITrigger trigger = TriggerBuilder
                .Create()
                .WithIdentity(jobDetails[0].Key.Name, jobDetails[0].Key.Group)
                .ForJob(jobDetails[0])
                .WithSimpleSchedule()
                .Build();
                await Scheduler.ScheduleJob(trigger);


                /*
                                // 增加 Listener
                                Scheduler.ListenerManager.AddJobListener(_jobListener);
                                Scheduler.ListenerManager.AddSchedulerListener(_schedulerListener);
                */

                // 逐一將工作項目加入排程器中 
                /*
                foreach (var jobSchedule in _allJobSchedules)
                {
                    var jobDetail = CreateJobDetail(jobSchedule);
                    var trigger = CreateTrigger(jobSchedule);

                    //var executingJob = Scheduler.GetCurrentlyExecutingJobs();
                    await Scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                    jobSchedule.JobStatus = JobStatus.Scheduled;
                }
                */


                // 啟動排程
                await Scheduler.Start(cancellationToken);
                //await StartB(Scheduler);
            }
        }

        /// <summary>
        /// 停止排程器
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Scheduler != null && !Scheduler.IsShutdown)
            {
                _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 非同步-停止排程器");
                await Scheduler.Shutdown(cancellationToken);
            }
        }

        /// <summary>
        /// 取得所有作業的最新狀態
        /// </summary>
        public async Task<IEnumerable<JobSchedule>> GetJobSchedules()
        {
            if (Scheduler.IsShutdown)
            {
                // 排程器停止時更新各工作狀態為停止
                foreach (var jobSchedule in _allJobSchedules)
                {
                    jobSchedule.JobStatus = JobStatus.Stopped;
                }
            }
            else
            {
                // 取得目前正在執行的 Job 來更新各 Job 狀態
                var executingJobs = await Scheduler.GetCurrentlyExecutingJobs();
                foreach (var jobSchedule in _allJobSchedules)
                {
                    var isRunning = executingJobs.FirstOrDefault(j => j.JobDetail.Key.Name == jobSchedule.JobName) != null;
                    jobSchedule.JobStatus = isRunning ? JobStatus.Running : JobStatus.Scheduled;
                }

            }

            return _allJobSchedules;
        }

        /// <summary>
        /// 手動觸發作業
        /// </summary>
        public async Task TriggerJobAsync(string jobName)
        {
            if (Scheduler != null && !Scheduler.IsShutdown)
            {
                //_logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - job{jobName} - group{jobGroup} - 非同步-手動觸發作業");
                await Scheduler.TriggerJob(new JobKey(jobName), CancellationToken);
            }
        }

        /// <summary>
        /// 手動中斷作業
        /// </summary>
        public async Task InterruptJobAsync(string jobName)
        {
            if (Scheduler != null && !Scheduler.IsShutdown)
            {
                var targetExecutingJob = await GetExecutingJob(jobName);
                if (targetExecutingJob != null)
                {
                    //_logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - job{jobName} - group{jobGroup}- InterruptJobAsync");
                    await Scheduler.Interrupt(new JobKey(jobName));
                }
            }
        }

        /// <summary>
        /// 取得特定執行中的作業
        /// </summary>
        private async Task<IJobExecutionContext> GetExecutingJob(string jobName)
        {
            if (Scheduler != null)
            {
                var executingJobs = await Scheduler.GetCurrentlyExecutingJobs();
                return executingJobs.FirstOrDefault(j => j.JobDetail.Key.Name == jobName);
            }
            return null;
        }

        /// <summary>
        /// 建立作業細節 (後續會透過 JobFactory 依此資訊從 DI 容器取出 Job 實體)
        /// </summary>
        private IJobDetail CreateJobDetail(JobSchedule jobSchedule)
        {
            var jobType = jobSchedule.JobType;
            var jobDetail = JobBuilder
                .Create(jobType)
                .WithIdentity(jobSchedule.JobName, jobSchedule.JobGroup)
                .WithDescription(jobType.Name)
                .Build();

            // 可以在建立 job 時傳入資料給 job 使用
            jobDetail.JobDataMap.Put("Payload", jobSchedule);

            return jobDetail;
        }

        /// <summary>
        /// 產生觸發器
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private ITrigger CreateTrigger(JobSchedule schedule)
        {
            if (schedule.CronExpression != null)
            {
                return TriggerBuilder.Create()
                .WithIdentity($"{schedule.JobName}.trigger")
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.CronExpression)
                .Build();
            }
            else
            {
                return TriggerBuilder
                .Create()
                .WithIdentity($"{schedule.JobName}.trigger")
                .WithSimpleSchedule()
                /*
                .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(5)
                .RepeatForever()
                )
                */
                .WithDescription("只執行一次")
                .Build();
            }
        }
    }
}
