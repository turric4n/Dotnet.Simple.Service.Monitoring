using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Simple.Service.Monitoring.Extensions;
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

            var monitoringDataService = new MonitoringDataService();

            // Register data service for sharing health reports
            services.AddSingleton(monitoringDataService);

            // Register RazorPages from the UI project
            services.AddRazorPages()
                .AddApplicationPart(typeof(IndexModel).Assembly);

            // Register the observer that will capture health reports
            monitoringConfigurationService.WithAdditionalPublisherObserver(new ReportObserver(monitoringDataService, true));

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
            // API endpoint for JSON data
            endpoints.MapGet("/monitoring-api", async context =>
            {
                var dataService = context.RequestServices.GetRequiredService<MonitoringDataService>();
                var healthChecks = dataService.GetAllHealthChecks();
                var overallStatus = dataService.GetOverallStatus();

                var response = new ExpandoObject();
                response.TryAdd("Status", overallStatus.ToString());
                response.TryAdd("LastUpdated", DateTime.UtcNow.ToString("o"));

                var checks = new List<ExpandoObject>();
                foreach (var check in healthChecks)
                {
                    var checkData = new ExpandoObject();
                    checkData.TryAdd("Name", check.Name);
                    checkData.TryAdd("Status", check.Status.ToString());
                    checkData.TryAdd("Description", check.Description);
                    checkData.TryAdd("Duration", check.Duration.TotalMilliseconds);
                    checkData.TryAdd("LastUpdated", check.LastUpdated.ToString("o"));

                    // Add any additional data if available in the entry
                    if (check.Entry.Data?.Count > 0)
                    {
                        var data = new ExpandoObject();
                        foreach (var dataItem in check.Entry.Data)
                        {
                            ((IDictionary<string, object>)data).TryAdd(dataItem.Key, dataItem.Value);
                        }
                        checkData.TryAdd("Data", data);
                    }

                    checks.Add(checkData);
                }

                response.TryAdd("HealthChecks", checks);

                context.Response.Headers.Add("Content-Type", "application/json");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            });

            // Map Razor Pages
            endpoints.MapRazorPages();

            return endpoints;
        }
    }
}