using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Simple.Service.Monitoring.UI;
using Simple.Service.Monitoring.UI.Models;
using Simple.Service.Monitoring.UI.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
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

            // Register data service for sharing health reports
            services.AddSingleton<MonitoringDataService>();

            // Register RazorPages from the UI project
            services.AddRazorPages()
                .AddApplicationPart(typeof(IndexModel).Assembly);

            // Get or register the monitoring data service
            var serviceProvider = services.BuildServiceProvider();
            var dataService = serviceProvider.GetRequiredService<MonitoringDataService>();

            // Register the observer that will capture health reports
            monitoringConfigurationService.WithAdditionalPublisherObserver(new ReportObserver(dataService, true));

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
                var reports = dataService.GetHealthReports();

                var reportsList = new List<ExpandoObject>();

                foreach (var healthReport in reports)
                {
                    var report = new ExpandoObject();

                    report.TryAdd("Status", healthReport.Status.ToString());
                    report.TryAdd("TotalDuration", healthReport.TotalDuration.ToString());

                    var entries = new List<ExpandoObject>();

                    foreach (var entry in healthReport.Entries)
                    {
                        var entryReport = new ExpandoObject();
                        entryReport.TryAdd("Name", entry.Key);
                        entryReport.TryAdd("Status", entry.Value.Status.ToString());
                        entryReport.TryAdd("Description", entry.Value.Description);
                        entryReport.TryAdd("Duration", entry.Value.Duration.ToString());
                        {
                            var data = new ExpandoObject();
                            foreach (var dataItem in entry.Value.Data)
                            {
                                ((IDictionary<string, object>)data).TryAdd(dataItem.Key, dataItem.Value);
                            }
                            entryReport.TryAdd("Data", data);
                        }
                        entries.Add(entryReport);
                    }

                    report.TryAdd("Entries", entries);
                    reportsList.Add(report);
                }

                context.Response.Headers.Add("Content-Type", "application/json");
                await context.Response.WriteAsync(JsonSerializer.Serialize(reportsList));
            });

            // Map Razor Pages
            endpoints.MapRazorPages();

            return endpoints;
        }
    }
}