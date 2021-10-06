﻿#region 排程器說明
//正在嘗試進行排他，首先先上了每個Job的群組名稱，打算用這群組名稱來限制另一個群組名稱的等待
#endregion

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;
using Quartz.Spi;
using QuartzTests.DTOs;
using QuartzTests.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzTests.Services
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;

        private readonly ILogger<QuartzHostedService> _logger;

        private readonly IEnumerable<JobSchedule> _injectJobSchedules;

        private List<JobSchedule> _allJobSchedules;

        //private readonly IJobListener _jobListener;

        //private readonly ISchedulerListener _schedulerListener;

        public IScheduler Scheduler { get; set; }

        public CancellationToken CancellationToken { get; private set; }

        public QuartzHostedService(ILogger<QuartzHostedService> logger, ISchedulerFactory schedulerFactory, IJobFactory jobFactory, IEnumerable<JobSchedule> jobSchedules/*,IJobListener jobListener, ISchedulerListener schedulerListener*/)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            //_jobListener = jobListener ?? throw new ArgumentNullException(nameof(jobListener));
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

                
                _allJobSchedules.Add(new JobSchedule(jobName: "任務編號 1-1", jobGroup: "工作群組 1 ", jobType: typeof(WorkingForLongTime)));
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
                IJobDetail job1 = JobBuilder.Create<WorkingForLongTime>().WithIdentity("任務編號 1-1", "工作群組 1 ").Build();
                IJobDetail job2 = JobBuilder.Create<WorkingForLongTime>().WithIdentity("任務編號 2-1", "工作群組 2 ").Build();

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
                Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                Scheduler.JobFactory = _jobFactory;


                /*
                                // 增加 Listener
                                Scheduler.ListenerManager.AddJobListener(_jobListener);
                                Scheduler.ListenerManager.AddSchedulerListener(_schedulerListener);
                */

                // 逐一將工作項目加入排程器中 

                foreach (var jobSchedule in _allJobSchedules)
                {
                    var jobDetail = CreateJobDetail(jobSchedule);
                    var trigger = CreateTrigger(jobSchedule);

                    //var executingJob = Scheduler.GetCurrentlyExecutingJobs();
                    await Scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                    jobSchedule.JobStatus = JobStatus.Scheduled;
                }

                //JobChainingJobListener jobChainer = new JobChainingJobListener("1to2");
                //jobChainer.AddJobChainLink(new JobKey("任務編號 1-1"), new JobKey("任務編號 2-1"));

                // 啟動排程
                await Scheduler.Start(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(2));

                await StartB(Scheduler);
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
        /// <summary>
        /// 測試開始另一個排程，排他，很白癡做法
        /// </summary>
        /// <param name="scheduler1"></param>
        /// <returns></returns>
        public static async Task StartB(IScheduler scheduler1/*, IScheduler scheduler2*/)
        {
            Console.WriteLine("開始嘗試執行第二個工作↓");
            Console.WriteLine();

            IJobDetail job = JobBuilder.Create<WorkingForLongTime>()
                .WithIdentity("等待3", "group1")
                .Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger2", "group1")//識別設定
                .StartNow()//開始時間設定，這個是現在，還有StartAt(UTC時間)可以使用
                .WithSimpleSchedule()
                .Build();

            if (CheckJobs(scheduler1, "任務編號 1-1"))//如果1號排程器正在執行任何工作
            {
                do
                {
                    Console.WriteLine("[X] 1號排程器還在執行工作... >>>任務編號 1-1");
                    await Task.Delay(TimeSpan.FromSeconds(3));//等三秒
                } while (CheckJobs(scheduler1, "任務編號 1-1"));//如果1號排程器還在執行任何工作，那就在一次DO
                //如果1號沒執行任何工作，那就會執行下列程式碼
                await scheduler1.ScheduleJob(job, trigger);
                //await scheduler1.Start();
                Console.WriteLine("[O] 開始執行二號工作!");
                /*
                                Console.WriteLine("1結束");
                                Console.WriteLine("2號START" + scheduler2.IsStarted);
                                Console.WriteLine("2號STAND" + scheduler2.InStandbyMode);
                                Console.WriteLine("2號OFF" + scheduler2.IsShutdown);
                */
            }
            else
            {
                Console.WriteLine("[O] 1號排程器沒有工作");
                await scheduler1.ScheduleJob(job, trigger);
                //await scheduler1.Start();
                Console.WriteLine("[O] 2號排程器開始執行工作!");
            }
        }
        /// <summary>
        /// 檢查這個排程器裡面的正在執行工作資料，並依傳入的任務名稱(要被排他的)來做判斷基準，若傳入的任務名稱正在執行，會回傳True，反之False
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="targetName"></param>
        /// <returns></returns>

        private static bool CheckJobs(IScheduler scheduler, string targetName)
        {
            List<string> jobNames = new List<string>();
            List<string> jobGroups = new List<string>();

            foreach (var x in scheduler.GetCurrentlyExecutingJobs().Result)
            {
                jobNames.Add(x.JobDetail.Key.Name);
                if (!jobGroups.Contains(x.JobDetail.Key.Group))
                {
                    jobGroups.Add(x.JobDetail.Key.Group);
                }
            }
            Console.WriteLine("正在執行的任務名稱");
            foreach (var item in jobNames)
            {
                Console.Write(" | " + item);
            }
            Console.WriteLine();

            Console.WriteLine("正在執行工作的群組們");
            foreach (var item in jobGroups)
            {
                Console.Write(" | " + item);
            }
            Console.WriteLine();

            if (jobNames.Contains(targetName))
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