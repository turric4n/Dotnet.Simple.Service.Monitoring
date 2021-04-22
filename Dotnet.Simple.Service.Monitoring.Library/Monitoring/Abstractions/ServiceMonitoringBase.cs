using System;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class ServiceMonitoringBase : IServiceMonitoring
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected readonly Guid _monitorId;
        protected readonly string _name;
        
        protected ServiceMonitoringBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
            _monitorId = new Guid();
            _name = healthCheck.Name;
        }

        protected internal abstract void Validate();

        public void SetUp()
        {
            Validate();
            SetMonitoring();
            SetPublishing();
        }

        protected internal abstract void SetMonitoring();

        protected internal void SetPublishing()
        {
            var hastoalert = (_healthCheck.AlertBehaviour != null && _healthCheck.AlertBehaviour.Alert);

            if (hastoalert)
            {
                switch (_healthCheck.AlertBehaviour.AlertTransport)
                {
                    case AlertTransport.Dummy:
                        _healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
                        {
                            return new DummyPublisher();
                        });
                        break;
                    case AlertTransport.Email:
                        _healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
                        {
                            return new DummyPublisher();
                        });
                        break;
                    case AlertTransport.Telegram:
                        break;
                    case AlertTransport.Slack:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
