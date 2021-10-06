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
using QuartzTests.Services;
using System;
using System.Collections.Generic;
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
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

            //�VDI�e�����UJob
            services.AddSingleton<WorkingForLongTime>();

            //�VDI�e�����UJobSchedule
            //services.AddSingleton(new JobSchedule(jobName: "0,30*n��", jobType: typeof(WorkingForLongTime), cronExpression: "0/30 * * * * ?"));
            //services.AddSingleton(new JobSchedule(jobName: "0,52*n��", jobType: typeof(WorkingForLongTime), cronExpression: "0/52 * * * * ?"));

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
    }
}
