#region Job說明
//這個Job會重複執行自己內容5次並Console.Log
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzTests.DTOs;

namespace QuartzTests.Jobs
{
    [DisallowConcurrentExecution]
    public class WorkingForLongTime : IJob
    {
        private readonly ILogger<WorkingForLongTime> _logger;
        private readonly IServiceProvider _provider;

        public WorkingForLongTime(ILogger<WorkingForLongTime> logger, IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public Task Execute(IJobExecutionContext context)
        {
            var schedule = context.JobDetail.JobDataMap.Get("Payload") as JobSchedule;
            if(schedule != null)
            {
            var jobName = schedule.JobName ?? "預設工作名稱~~";
            var jobGroup = schedule.JobGroup ?? "預設群組名稱~~";
            using (var scope = _provider.CreateScope())
            {
                // 如果要使用到 DI 容器中定義為 Scope 的物件實體時，由於 Job 定義為 singleton
                // 因此無法直接取得 Scope 的實體，此時就需要於 CreateScope 在 scope 中產生該實體
                // ex. var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
            }

            _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 工作名稱：{jobName} - group{jobGroup}- 開始");

            for (int i = 1; i <= 5; i++)
            {

                // 自己定義當 job 要被迫被被中斷時，哪邊適合結束
                // 如果沒有設定，當作業被中斷時，並不會真的中斷，而會整個跑完
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                System.Threading.Thread.Sleep(1000);
                _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 工作名稱：{jobName} - group{jobGroup}- 第{i}/5次");

            }


            _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 工作名稱：{jobName} - group{jobGroup}- 完成");
            }else
            {
                _logger.LogInformation($"@{DateTime.Now:HH:mm:ss} - 沒名稱的工作完成：");
            }
            return Task.CompletedTask;
        }
    }
}
