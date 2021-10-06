using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Logging;
using Quartz.Spi;
using QuartzTest.Library;

namespace QuartzTest
{
    public class HelloJob : IJob
    {
        //一個Job的執行內容
        public async Task Execute(IJobExecutionContext context)
        {
            //Print出字
            await Console.Out.WriteLineAsync("###工作開始摟###");
            //等待十秒後
            await Task.Delay(TimeSpan.FromSeconds(10));
            //Print出Done
            await Console.Out.WriteLineAsync("###工作結束了###");
        }
    }
    class Program
    {
        private static async Task Main(string[] args)
        {
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            //製作並取得排程器的物件

            StdSchedulerFactory factory = new StdSchedulerFactory();
            //DirectSchedulerFactory factory1 = new DirectSchedulerFactory(); //這個可以自定義名字跟ID等，但是需要準備很多東西
            IScheduler scheduler1 = await factory.GetScheduler(); //製作排程器1號
            IScheduler scheduler2 = await factory.GetScheduler();//製作排程器2號
            Console.WriteLine();
            Console.WriteLine("排程器1號名稱：" + scheduler1.SchedulerName);
            Console.WriteLine("排程器2號名稱：" + scheduler2.SchedulerName);
            var AllSchedulers = factory.GetAllSchedulers(); //取得所有排程器名稱

            Console.WriteLine("***所有排程器名稱↓***");
            foreach (var x in AllSchedulers.Result) //顯示所有排程器名稱，可能名字重複的關係，所以他只顯示一個
            {
                Console.WriteLine(x.GetHashCode());
            }
            Console.WriteLine();

            //定義一個Job(工作)，使用HelloJob類別定義的東西來設定執行的內容(這個類別我先寫在此Class的最上面方便更改，之後會分成其他Class檔案)
            IJobDetail job = JobBuilder.Create<HelloJob>()
                .WithIdentity("任務名稱：等待10秒", "group1") //WithIdentity來設定他的辨別標籤，Build來取得這個Job的物件
                .Build();

            IJobDetail job2 = JobBuilder.Create<HelloJob>()
                .WithIdentity("任務名稱：1", "group1") //WithIdentity來設定他的辨別標籤，Build來取得這個Job的物件
                .Build();

            IJobDetail job3 = JobBuilder.Create<HelloJob>()
                .WithIdentity("任務名稱：2") //WithIdentity來設定他的辨別標籤，Build來取得這個Job的物件
                .Build();

            //定義一個Trigger(觸發器)
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")//WithIdentity設定他的辨別標籤
                .StartNow()
                .WithSimpleSchedule()
                .Build();

            ITrigger trigger2 = TriggerBuilder.Create()
                .WithIdentity("trigger2", "group1")//WithIdentity設定他的辨別標籤
                .StartNow()
                .WithSimpleSchedule()
                .Build();

            ITrigger trigger3 = TriggerBuilder.Create()
                .WithIdentity("trigger3", "group1")//WithIdentity設定他的辨別標籤
                .StartNow()
                .WithSimpleSchedule()
                .Build();
            
            await scheduler1.ScheduleJob(job, trigger); //要求排程器使用此Trigger來觸發Job
            await scheduler2.ScheduleJob(job2, trigger2); //要求排程器使用此Trigger來觸發Job
            await scheduler2.ScheduleJob(job3, trigger3); //要求排程器使用此Trigger來觸發Job
            await scheduler2.Start();
            Console.WriteLine("XXX");
            
            foreach (var x in await scheduler2.GetCurrentlyExecutingJobs())
            {
                Console.WriteLine("●工作內容詳細：" + x.JobDetail);
                //Console.WriteLine("x.Result：" + x.Result);
                //Console.WriteLine("x.FireInstanceId：" + x.FireInstanceId);
                //Console.WriteLine("x.MergedJobDataMap：" + x.MergedJobDataMap);
            }
            Console.WriteLine("XXX");

            await scheduler1.Start();//執行1號排程器
            //也是可以有多個Trigger，像下面這行
            // await scheduler1.ScheduleJob(job, new List<ITrigger>() { trigger1, trigger2 }, replace: true);

            //因為我想看現在正在執行的工作，所以讓他Delay了幾秒
            await Task.Delay(TimeSpan.FromSeconds(2));

            //從排程器取得正在執行的工作
            var executingJobs = scheduler1.GetCurrentlyExecutingJobs();
            Console.WriteLine("-------------------------");
            Console.WriteLine("正在執行的工作數量："+executingJobs.Result.Count);//print出現在執行的工作數量
            //print每個工作內容詳細資料
            foreach (var x in executingJobs.Result)
            {
                Console.WriteLine("●工作內容詳細：" + x.JobDetail);
                //Console.WriteLine("x.Result：" + x.Result);
                //Console.WriteLine("x.FireInstanceId：" + x.FireInstanceId);
                //Console.WriteLine("x.MergedJobDataMap：" + x.MergedJobDataMap);
            }
            Console.WriteLine("-------------------------");

            //嘗試開始其他工作，這個Function會去檢查前者有沒有正在執行工作，有的話就等到他完成後才開始
            await StartB(scheduler1, scheduler2, factory);

            //100秒後停止
            await Task.Delay(TimeSpan.FromSeconds(100));
            //等待任務完成後關閉排程器
            await scheduler1.Shutdown(true);

            await Task.Delay(TimeSpan.FromSeconds(2000));
        }
        public static async Task StartB(IScheduler scheduler1,IScheduler scheduler2, StdSchedulerFactory factory)
        {
            Console.WriteLine("開始嘗試執行第二個工作↓");
            Console.WriteLine();

            IJobDetail job = JobBuilder.Create<HelloJob>()
                .WithIdentity("等待3", "group1")
                .Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger2", "group1")//識別設定
                .StartNow()//開始時間設定，這個是現在，還有StartAt(UTC時間)可以使用
                .WithSimpleSchedule()
                .Build();

            if (CheckJobs(scheduler1, "任務名稱：2"))//如果1號排程器正在執行任何工作
            {
                do
                {
                    Console.WriteLine("[X] 1號排程器還在執行工作... >>>任務名稱：2");
                    await Task.Delay(TimeSpan.FromSeconds(3));//等三秒
                } while (CheckJobs(scheduler1, "任務名稱：2"));//如果1號排程器還在執行任何工作，那就在一次DO
                //如果1號沒執行任何工作，那就會執行下列程式碼
                await scheduler2.ScheduleJob(job, trigger);//指定2號排程器的Job
                await scheduler2.Start();//執行2號排程器
                Console.WriteLine("[O] 2號排程器開始執行工作!");
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
                await scheduler2.ScheduleJob(job, trigger);
                await scheduler2.Start();
                Console.WriteLine("[O] 2號排程器開始執行工作!");
            }
        }

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

        private class ConsoleLogProvider : ILogProvider
        {
            //顯示程式詳細執行情況
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    if (level >= LogLevel.Info && func != null)
                    {
                        Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                    }
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
            {
                throw new NotImplementedException();
            }
        }
        
    }
}
