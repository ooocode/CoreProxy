using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
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
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                var jobKey = new JobKey(nameof(Local));

                //// base quartz scheduler, job and trigger configuration
                q.AddJob<Local>(jobKey).AddTrigger(t =>
                {
                    t.StartNow().WithSimpleSchedule(x => x
                       .WithInterval(System.TimeSpan.FromMilliseconds(1))
                       .RepeatForever())
                       .ForJob(jobKey);
                });
            });

            services.AddQuartzServer(options =>
            {
                options.WaitForJobsToComplete = true;
            });
        }



        public void Configure(IApplicationBuilder app)
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings.Add(".pac", "application/octet-stream");
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });
        }
    }
}
