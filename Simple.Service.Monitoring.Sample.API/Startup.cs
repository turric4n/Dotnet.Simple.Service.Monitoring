using Elastic.Clients.Elasticsearch.IndexLifecycleManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.SignalRPublisher;
using Simple.Service.Monitoring.Library.Options;
using Simple.Service.Monitoring.Sample.API.External;
using System.Collections.Generic;

namespace Simple.Service.Monitoring.Sample.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            services.AddHealthChecks()
                .AddCheck("test_healthy", () => HealthCheckResult.Healthy("Test is healthy"))
                .AddCheck("test_degraded", () => HealthCheckResult.Degraded("Test is degraded"))
                .AddCheck("test_unhealthy", () => HealthCheckResult.Unhealthy("Test is unhealthy"));

            services.AddTransient<IExternalService, ExternalService>();

            var runtimeSettings = new MonitorOptions();

            var signalRSettings = new SignalRTransportSettings
            {
                Name = "MonitoringHub",
                HubMethod = "SendHealthChecks",
                HubUrl = "http://localhost:5001/monitoringhub",
            };

            runtimeSettings.SignalRTransportSettings = new List<SignalRTransportSettings>();

            runtimeSettings.SignalRTransportSettings.Add(signalRSettings);

            runtimeSettings.HealthChecks = new List<ServiceHealthCheck>()
            {
                new ServiceHealthCheck()
                {
                    Alert = true,
                    Name = "Testing http",
                    ServiceType = ServiceType.Http,
                    EndpointOrHost = "https://isnotworking",
                },
                new ServiceHealthCheck()
                {
                    Alert = true,
                    Name = "All healthChecks interceptor",
                    ServiceType = ServiceType.Interceptor,
                    AlertBehaviour = new List<AlertBehaviour>()
                    {
                        new AlertBehaviour
                        {
                            PublishAllResults = true,
                            TransportMethod = AlertTransportMethod.SignalR,
                            TransportName = "MonitoringHub"
                        }
                    },
                }
            };

            services
                .AddServiceMonitoring(Configuration)
                .WithRuntimeSettings(runtimeSettings);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
