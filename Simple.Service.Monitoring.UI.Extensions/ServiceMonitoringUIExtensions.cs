using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.UI.Hubs;
using Simple.Service.Monitoring.UI.Models;
using Simple.Service.Monitoring.UI.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceMonitoringUiExtensions
    {
        public static IServiceMonitoringConfigurationService WithServiceMonitoringUi(this IServiceMonitoringConfigurationService monitoringConfigurationService, IServiceCollection services)
        {
            if (monitoringConfigurationService == null)
            {
                throw new ArgumentNullException(nameof(monitoringConfigurationService), "ServiceMonitoringConfigurationService cannot be null");
            }

            // Add SignalR services
            services.AddSignalR();

            // Register data service for sharing health reports
            services.AddSingleton<IMonitoringDataService, MonitoringDataService>();

            // Register RazorPages from the UI project
            services.AddRazorPages()
                .AddApplicationPart(typeof(IndexModel).Assembly);

            // Register the observer that will capture health reports

            return monitoringConfigurationService;
        }

        public static IApplicationBuilder UseServiceMonitoringUi(this IApplicationBuilder app)
        {
            // Serve static files from the UI project
            var uiAssembly = typeof(IndexModel).Assembly;
            var embeddedFileProvider = new EmbeddedFileProvider(
                uiAssembly, "Simple.Service.Monitoring.UI.wwwroot");

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = embeddedFileProvider,
                RequestPath = "/monitoring-static"
            });

            return app;
        }

        public static IEndpointRouteBuilder MapServiceMonitoringUi(this IEndpointRouteBuilder endpoints)
        {
            // Map Razor Pages
            endpoints.MapRazorPages();

            // Map SignalR Hub
            endpoints.MapHub<MonitoringHub>("/monitoringhub");

            return endpoints;
        }
    }
}
