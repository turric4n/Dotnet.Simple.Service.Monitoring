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
using Simple.Service.Monitoring.UI.Options;
using Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Configuration;
using Simple.Service.Monitoring.UI.Repositories.LiteDb;
using Simple.Service.Monitoring.UI.Repositories.Memory;
using Simple.Service.Monitoring.UI.Repositories.SQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceMonitoringUiExtensions
    {
        public static IServiceMonitoringConfigurationService WithServiceMonitoringUi(this IServiceMonitoringConfigurationService monitoringConfigurationService, 
            IServiceCollection services, IConfiguration configuration)
        {
            if (monitoringConfigurationService == null)
            {
                throw new ArgumentNullException(nameof(monitoringConfigurationService), "ServiceMonitoringConfigurationService cannot be null");
            }

            services.AddSignalR();

            services.AddSingleton<IMonitoringDataService, MonitoringDataService>();

            var monitoringSection = configuration.GetSection("MonitoringUi");

            services
                .AddOptions<MonitoringUiOptions>()
                .Bind(monitoringSection)
                .ValidateOnStart();

            services.AddSingleton<IMonitoringDataRepositoryLocator, MonitoringDataRepositoryLocator>();

            services.AddRazorPages()
                .AddApplicationPart(typeof(IndexModel).Assembly);

            services.AddKeyedSingleton<IMonitoringDataRepository, SqlMonitoringDataRepository>("Sql");

            services.AddKeyedSingleton<IMonitoringDataRepository, LiteDbMonitoringDatarepository>("LiteDb");

            services.AddKeyedSingleton<IMonitoringDataRepository, InMemoryMonitoringDataRepository>("InMemory");

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
