using CuttingEdge.Conditions;
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class DockerServiceMonitoring : ServiceMonitoringBase
    {
        public DockerServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .Requires(HealthCheck.HealthCheckConditions.DockerBehaviour.ContainerNameOrId)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var behaviour = HealthCheck.HealthCheckConditions.DockerBehaviour;
            var timeout = TimeSpan.FromMilliseconds(behaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new DockerHealthCheck(behaviour.ContainerNameOrId, behaviour.DockerEndpoint, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class DockerHealthCheck : IHealthCheck
    {
        private readonly string _containerNameOrId;
        private readonly string _dockerEndpoint;
        private readonly TimeSpan _timeout;

        public DockerHealthCheck(string containerNameOrId, string dockerEndpoint, TimeSpan timeout)
        {
            _containerNameOrId = containerNameOrId ?? throw new ArgumentNullException(nameof(containerNameOrId));
            _dockerEndpoint = dockerEndpoint;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            DockerClient client = null;
            try
            {
                var config = new DockerClientConfiguration(
                    string.IsNullOrEmpty(_dockerEndpoint)
                        ? null
                        : new Uri(_dockerEndpoint));

                client = config.CreateClient();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);

                var containerInspect = await client.Containers.InspectContainerAsync(_containerNameOrId, cts.Token);

                if (!containerInspect.State.Running)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Container '{_containerNameOrId}' is not running. Status: {containerInspect.State.Status}");
                }

                if (containerInspect.State.Health != null)
                {
                    var healthStatus = containerInspect.State.Health.Status;
                    if (healthStatus == "unhealthy")
                    {
                        return HealthCheckResult.Unhealthy(
                            $"Container '{_containerNameOrId}' health status: {healthStatus}");
                    }
                    if (healthStatus == "starting")
                    {
                        return HealthCheckResult.Degraded(
                            $"Container '{_containerNameOrId}' is still starting");
                    }
                }

                return HealthCheckResult.Healthy(
                    $"Container '{_containerNameOrId}' is running. Status: {containerInspect.State.Status}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Docker container check failed: {ex.Message}", ex);
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}
