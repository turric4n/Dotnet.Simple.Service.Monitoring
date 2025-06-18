using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;

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
                .WithApplicationSettings()
                .WithServiceMonitoringUi(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseServiceMonitoringUi();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapServiceMonitoringUi();

                //endpoints.MapGet("/", async context =>
                //{
                //    var stackMonitoring = context.RequestServices.GetRequiredService<IStackMonitoring>();

                //    var monitors = stackMonitoring.GetMonitors();
                //    var publishers = stackMonitoring.GetPublishers();

                //    await context.Response.WriteAsync($"Monitors : {JsonConvert.SerializeObject(monitors)} \r\n Publishers : {JsonConvert.SerializeObject(publishers)}");
                //});
            });
        }
    }
}
