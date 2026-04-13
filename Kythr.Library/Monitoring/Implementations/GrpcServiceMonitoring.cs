using CuttingEdge.Conditions;
using Grpc.Health.V1;
using Grpc.Net.Client;
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
    public class GrpcServiceMonitoring : ServiceMonitoringBase
    {
        public GrpcServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(HealthCheck.EndpointOrHost, UriKind.Absolute))
                .IsTrue();
        }

        protected internal override void SetMonitoring()
        {
            var endpoint = HealthCheck.EndpointOrHost;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.GrpcBehaviour.TimeOutMs);
            var useHealthProtocol = HealthCheck.HealthCheckConditions.GrpcBehaviour.UseHealthCheckProtocol;

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new GrpcHealthCheck(endpoint, timeout, useHealthProtocol);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class GrpcHealthCheck : IHealthCheck
    {
        private readonly string _endpoint;
        private readonly TimeSpan _timeout;
        private readonly bool _useHealthProtocol;

        public GrpcHealthCheck(string endpoint, TimeSpan timeout, bool useHealthProtocol)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _timeout = timeout;
            _useHealthProtocol = useHealthProtocol;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            GrpcChannel channel = null;
            try
            {
                channel = GrpcChannel.ForAddress(_endpoint);

                if (_useHealthProtocol)
                {
                    var client = new Health.HealthClient(channel);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_timeout);

                    var response = await client.CheckAsync(new HealthCheckRequest(), cancellationToken: cts.Token);

                    return response.Status switch
                    {
                        HealthCheckResponse.Types.ServingStatus.Serving =>
                            HealthCheckResult.Healthy("gRPC service is serving"),
                        HealthCheckResponse.Types.ServingStatus.NotServing =>
                            HealthCheckResult.Unhealthy("gRPC service is not serving"),
                        _ =>
                            HealthCheckResult.Degraded($"gRPC service status: {response.Status}")
                    };
                }
                else
                {
                    var client = new Health.HealthClient(channel);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_timeout);

                    await client.CheckAsync(new HealthCheckRequest(), cancellationToken: cts.Token);
                    return HealthCheckResult.Healthy("gRPC channel connected successfully");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"gRPC connection failed: {ex.Message}", ex);
            }
            finally
            {
                if (channel != null)
                    await channel.ShutdownAsync();
            }
        }
    }
}
