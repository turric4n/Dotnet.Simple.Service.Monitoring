using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.UI.Hubs;
using Simple.Service.Monitoring.UI.Models;
using Simple.Service.Monitoring.UI.Services;
using System;
using Simple.Service.Monitoring.UI.Repositories;
using Simple.Service.Monitoring.UI.Settings;

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

            services.AddOptions<MonitoringUiSettings>();

            services.AddSingleton<IMonitoringDataRepositoryLocator, MonitoringDataRepositoryLocator>();

            // Register TagHelpers from the UI assembly
            services.AddRazorPages()
                .AddApplicationPart(typeof(IndexModel).Assembly);
            
            // Tag helpers will be auto-discovered from the assembly

            // Register the observer that will capture health reports

            return monitoringConfigurationService;
        }

        public static IApplicationBuilder UseServiceMonitoringUi(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment)
        {
            // Serve static files from the UI project
            var uiAssembly = typeof(IndexModel).Assembly;
            var embeddedFileProvider = new EmbeddedFileProvider(
                uiAssembly, "Simple.Service.Monitoring.UI.wwwroot");

            if (webHostEnvironment.IsDevelopment())
            {
                Simple.Service.Monitoring.UI.Helpers.AssetHelper.EnableCaching = false;
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = embeddedFileProvider,
                RequestPath = "/monitoring-static",
                // Add cache control headers for better caching behavior
                OnPrepareResponse = ctx =>
                {
                    // Set cache control headers
                    var headers = ctx.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(365) // Long cache for hashed assets
                    };
                }
            });

            app
                .ApplicationServices
                .GetService<IMonitoringDataService>()?.Init();

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
