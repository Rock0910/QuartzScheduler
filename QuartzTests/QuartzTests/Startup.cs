using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzTests.DTOs;
using QuartzTests.Factory;
using QuartzTests.Jobs;
using QuartzTests.Listeners;
using QuartzTests.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzTests
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();


            //向DI容器註冊Quartz服務
            services.AddSingleton<IJobFactory, JobFactory>();
            //services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobListener, JobListener>();

            //向DI容器註冊Job
            services.AddSingleton<WorkingForLongTime>();

            //向DI容器註冊JobSchedule
            //services.AddSingleton(new JobSchedule(jobName: "0,30*n啟", jobType: typeof(WorkingForLongTime), cronExpression: "0/30 * * * * ?"));
            //services.AddSingleton(new JobSchedule(jobName: "0,52*n啟", jobType: typeof(WorkingForLongTime), cronExpression: "0/52 * * * * ?"));

            services.AddSingleton(provider=> GetScheduler().Result);


            //向DI容器註冊Host服務
            services.AddSingleton<QuartzHostedService>();
            services.AddHostedService(provider => provider.GetService<QuartzHostedService>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }

        private async Task<IScheduler> GetScheduler()
        {
            NameValueCollection pars = new NameValueCollection
            {
                //scheduler名字
                ["quartz.scheduler.instanceName"] = "MyScheduler",
                //執行緒池個數
                ["quartz.threadPool.threadCount"] = "20",
                //型別為JobStoreXT,事務
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                //JobDataMap中的資料都是字串
                //["quartz.jobStore.useProperties"] = "true",
                //資料來源名稱
                ["quartz.jobStore.dataSource"] = "myDS",
                //資料表名字首
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                //使用Sqlserver的Ado操作代理類
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
                //資料來源連線字串
                ["quartz.dataSource.myDS.connectionString"] = "Server=127.0.0.1;Port=5432;Database=QuartzDB;User Id=postgres;Password=100988;",
                //資料來源的資料庫
                ["quartz.dataSource.myDS.provider"] = "Npgsql",
                //序列化型別
                ["quartz.serializer.type"] = "json",//binary
                //自動生成scheduler例項ID，主要為了保證叢集中的例項具有唯一標識
                ["quartz.scheduler.instanceId"] = "AUTO",
                //是否配置叢集
                ["quartz.jobStore.clustered"] = "true",
                [ "quartz.serializer.type"] = "json"
            };
            var schedulerFactory = new StdSchedulerFactory(pars);
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
            return scheduler;
        }
    }
}
