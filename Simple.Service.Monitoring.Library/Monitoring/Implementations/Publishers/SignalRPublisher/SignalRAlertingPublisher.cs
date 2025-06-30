using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.SignalRPublisher
{
    public class SignalRAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly SignalRTransportSettings _signalRTransportSettings;
        private HubConnection _hubConnection;
        private bool _disposed = false;

        public SignalRAlertingPublisher(
            IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _signalRTransportSettings = (SignalRTransportSettings)alertTransportSettings;
        }

        private async Task<HubConnection> GetHubConnectionAsync()
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                return _hubConnection;

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            // We need to resolve the hub URL from the settings
            var hubUrl = _signalRTransportSettings.HubUrl;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            try
            {
                await _hubConnection.StartAsync();
                return _hubConnection;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to connect to SignalR hub: {ex.Message}");
                return null;
            }
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                var connection = await GetHubConnectionAsync();
                if (connection == null || connection.State != HubConnectionState.Connected)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to connect to SignalR hub");
                    return;
                }

                // Convert health entries to HealthCheckData objects
                var healthCheckDataList = report.Entries.Select(e => 
                    new HealthCheckData(e.Value, e.Key)).ToList();


                // Send the alert through SignalR
                await connection.InvokeAsync(
                    _signalRTransportSettings.HubMethod,
                    healthCheckDataList,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the exception, but don't throw to avoid breaking health checks
                System.Diagnostics.Debug.WriteLine($"Failed to publish health report to SignalR: {ex.Message}");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<SignalRValidationError>()
                .Requires(_signalRTransportSettings.HubUrl)
                .IsNotNullOrWhiteSpace();

            Condition.WithExceptionOnFailure<SignalRValidationError>()
                .Requires(_signalRTransportSettings.HubMethod)
                .IsNotNullOrWhiteSpace();
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_hubConnection != null)
                {
                    _hubConnection.DisposeAsync().GetAwaiter().GetResult();
                    _hubConnection = null;
                }
            }

            _disposed = true;
        }

        ~SignalRAlertingPublisher()
        {
            Dispose(false);
        }
    }

    public class SignalRValidationError : Exception
    {
        public SignalRValidationError(string message) : base(message) { }
    }
}
