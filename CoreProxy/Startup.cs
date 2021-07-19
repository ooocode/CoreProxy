using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;


namespace CoreProxy
{
    public class Startup
    {
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddQuartz(q =>
            //{
            //    var jobKey = new JobKey(nameof(SysncActiveDirectoryJob));

            //    //// base quartz scheduler, job and trigger configuration
            //    q.AddJob<SysncActiveDirectoryJob>(jobKey).AddTrigger(t =>
            //    {
            //        t.StartNow().WithSimpleSchedule(x => x
            //           .WithIntervalInSeconds(1)
            //           .RepeatForever())
            //           .ForJob(jobKey);
            //    });
            //    // base quartz scheduler, job and trigger configuration
            //});

            //// ASP.NET Core hosting
            //services.AddQuartzServer(options =>
            //{
            //    // when shutting down we want jobs to complete gracefully
            //    options.WaitForJobsToComplete = true;
            //});

            services.AddHostedService<Local>();
        }



        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
        }
    }
}
