using HealthCheckDemo.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//into to health checks in .net core tim corey
//https://www.youtube.com/watch?v=Kbfto6Y2xdw
namespace HealthCheckDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            //Configure Health Checks
            services.AddHealthChecks()
                //start by hard coding in a test health check and a response message
                .AddCheck("Foo service", () =>
                //HealthCheckResult.Healthy("The check of the foo service worked."));
                // change to this for the result to be unhealthy
                //HealthCheckResult.Unhealthy("The check of the foo service resulted in unhealthy"));
                // change to this for the result to be degraded - there's aproblem, but it's still working
                //we can also have tags which we can use to filter which checks run
                //although this is a simple lambda expression just to return degraded, in actual fact I can run any custom health check here, 
                // and return degraded if it means that the project is up, but just not running optimally.
                HealthCheckResult.Degraded("We're working but suboptimal - dependent on requirements"), new[] { "service" })
                //we can actually put these into different checks, will more than one it will return the worst one
                // however we can select which ones we want to prioritise using tags, maybe buzz service is less important than foo bar services
                .AddCheck("bar service", () => HealthCheckResult.Healthy("the bar service is healthy"), new[] { "service" })
                .AddCheck("buzz service", () => HealthCheckResult.Unhealthy("the buzz service is unhealthy"), new[] { "sql" })
                .AddCheck<CustomCheck>("Custom check", tags: new[] { "custom" });

            services.AddSingleton<WeatherForecastService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                //map the health check to the health endpoint - change this to give us more information
                endpoints.MapHealthChecks("/health", new HealthCheckOptions() 
                {
                    //format a json response for our health check
                    //whole object of information could be used to create
                    //a dedicated application
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                //create a different health check on a different endpoint
                endpoints.MapHealthChecks("/health/quick", new HealthCheckOptions()
                {
                    //could call a method, but in this case will just return false, which means it just does the basic healkth check
                    Predicate = _ => false
                });

                //use tag to just call the services healthchecks
                endpoints.MapHealthChecks("/health/services", new HealthCheckOptions()
                {
                    //runs health checks with the right tags
                    Predicate = reg => reg.Tags.Contains("service"),
                    //give me the full JSON
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/health/custom", new HealthCheckOptions()
                {
                    Predicate = reg => reg.Tags.Contains("custom")
                });

                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
