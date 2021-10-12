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


            //�VDI�e�����UQuartz�A��
            services.AddSingleton<IJobFactory, JobFactory>();
            //services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobListener, JobListener>();

            //�VDI�e�����UJob
            services.AddSingleton<WorkingForLongTime>();

            //�VDI�e�����UJobSchedule
            //services.AddSingleton(new JobSchedule(jobName: "0,30*n��", jobType: typeof(WorkingForLongTime), cronExpression: "0/30 * * * * ?"));
            //services.AddSingleton(new JobSchedule(jobName: "0,52*n��", jobType: typeof(WorkingForLongTime), cronExpression: "0/52 * * * * ?"));

            services.AddSingleton(provider=> GetScheduler().Result);


            //�VDI�e�����UHost�A��
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
                //scheduler�W�r
                ["quartz.scheduler.instanceName"] = "MyScheduler",
                //��������Ӽ�
                ["quartz.threadPool.threadCount"] = "20",
                //���O��JobStoreXT,�ư�
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                //JobDataMap������Ƴ��O�r��
                //["quartz.jobStore.useProperties"] = "true",
                //��ƨӷ��W��
                ["quartz.jobStore.dataSource"] = "myDS",
                //��ƪ�W�r��
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                //�ϥ�Sqlserver��Ado�ާ@�N�z��
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
                //��ƨӷ��s�u�r��
                ["quartz.dataSource.myDS.connectionString"] = "Server=127.0.0.1;Port=5432;Database=QuartzDB;User Id=postgres;Password=100988;",
                //��ƨӷ�����Ʈw
                ["quartz.dataSource.myDS.provider"] = "Npgsql",
                //�ǦC�ƫ��O
                ["quartz.serializer.type"] = "json",//binary
                //�۰ʥͦ�scheduler�Ҷ�ID�A�D�n���F�O���O�������Ҷ��㦳�ߤ@����
                ["quartz.scheduler.instanceId"] = "AUTO",
                //�O�_�t�m�O��
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
