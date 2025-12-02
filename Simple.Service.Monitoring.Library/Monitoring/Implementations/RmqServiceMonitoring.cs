using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class RmqServiceMonitoring : ServiceMonitoringBase
    {
        public RmqServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition.Requires(HealthCheck.EndpointOrHost)
                .IsNotNull();

            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(HealthCheck.EndpointOrHost, UriKind.Absolute))
                .IsTrue();
        }

        protected internal override void SetMonitoring()
        {
            // Create a custom health check that GUARANTEES connection disposal
            HealthChecksBuilder.AddCheck<RabbitMqConnectionHealthCheck>(
                HealthCheck.Name,
                HealthStatus.Unhealthy,
                GetTags());

            // Register the health check with the connection string
            HealthChecksBuilder.Services.AddSingleton(sp => 
                new RabbitMqConnectionHealthCheck(HealthCheck.EndpointOrHost));
        }
    }

    /// <summary>
    /// Custom RabbitMQ health check that ensures proper connection disposal
    /// </summary>
    internal class RabbitMqConnectionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public RabbitMqConnectionHealthCheck(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            IConnection connection = null;
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_connectionString),
                    AutomaticRecoveryEnabled = false, // Not needed for health checks
                    RequestedHeartbeat = TimeSpan.FromSeconds(10),
                    ContinuationTimeout = TimeSpan.FromSeconds(10),
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(10)
                };

                // Create connection
                connection = await factory.CreateConnectionAsync(cancellationToken);

                // Check if connection is open
                return connection.IsOpen 
                    ? HealthCheckResult.Healthy($"RabbitMQ connection is healthy") 
                    : HealthCheckResult.Unhealthy($"RabbitMQ connection is not open");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"RabbitMQ connection failed: {ex.Message}", ex);
            }
            finally
            {
                // GUARANTEED disposal - this is the critical part!
                if (connection != null)
                {
                    try
                    {
                        await connection.CloseAsync(cancellationToken: cancellationToken);
                        
                    }
                    finally
                    {
                        connection.Dispose();
                    }
                }
            }
        }
    }
}
