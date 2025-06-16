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

        public static IServiceMonitoringBuilder AddServiceMonitoringUi(this IServiceMonitoringBuilder monitoringBuilder)
        {
            if (monitoringBuilder == null)
            {
                throw new ArgumentNullException(nameof(monitoringBuilder), "ServiceMonitoringBuilder cannot be null");
            }

            monitoringBuilder.AddPublisherObserver(new GenericObserver());

            return monitoringBuilder;
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
