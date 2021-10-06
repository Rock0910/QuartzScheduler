using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzTest.Library
{
    public class JobListener : IJobListener
    {
        public string Name { get { return "JobListener"; } }
       
        Task IJobListener.JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine("準備開始執行：" + context.JobDetail.JobType.Name);
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        Task IJobListener.JobExecutionVetoed(IJobExecutionContext context, CancellationToken canationToken)
        {
            Console.WriteLine("執行被否決：" + context.JobDetail.JobType.Name);
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }

        Task IJobListener.JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken)
        {
            Console.WriteLine("執行已完成：" + context.JobDetail.JobType.Name);

            throw new NotImplementedException();
        }
    }
}
