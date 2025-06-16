using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Extensions;
using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceMonitoringUiExtensions
    {
        private static ConcurrentBag<HealthReport> _healthReports = new ConcurrentBag<HealthReport>();
        public class GenericObserver: IObserver<HealthReport>
        {
            public void OnCompleted()
            {
                // Handle completion logic if needed
            }
            public void OnError(Exception error)
            {
                // Handle error logic if needed
            }
            public void OnNext(HealthReport value)
            {
                _healthReports.Add(value);
            }
        }

        public static IServiceMonitoringConfigurationService WithServiceMonitoringUi(this IServiceMonitoringConfigurationService monitoringConfigurationService)
        {
            if (monitoringConfigurationService == null)
            {
                throw new ArgumentNullException(nameof(monitoringConfigurationService), "ServiceMonitoringConfigurationService cannot be null");
            }

            monitoringConfigurationService.WithAdditionalPublisherObserver(new GenericObserver());

            return monitoringConfigurationService;
        }

        public static IEndpointRouteBuilder MapServiceMonitoringUi(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/monitoring-ui", async context =>
            {
                foreach (var healthReport in _healthReports)
                {
                    context.Response.Headers.Add("Content-Type", "application/json");
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(healthReport));
                }
            });

            return endpoints;
        }
    }
}
