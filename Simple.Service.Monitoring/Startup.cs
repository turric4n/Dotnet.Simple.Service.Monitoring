using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Simple.Service.Monitoring
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
            services
                .AddServiceMonitoring(Configuration)
                .WithServiceMonitoringUi(services, Configuration)
                .WithApplicationSettings();

            //services.AddHealthChecks()
            //    .AddCheck("test_healthy", () => HealthCheckResult.Healthy("Test is healthy"))
            //    .AddCheck("test_degraded", () => HealthCheckResult.Degraded("Test is degraded"))
            //    .AddCheck("test_unhealthy", () => HealthCheckResult.Unhealthy("Test is unhealthy"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseServiceMonitoringUi(env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapServiceMonitoringUi();
            });
        }
    }
}
