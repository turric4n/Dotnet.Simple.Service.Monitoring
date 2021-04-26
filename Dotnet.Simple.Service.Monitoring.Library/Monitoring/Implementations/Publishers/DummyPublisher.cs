using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers
{
    public class DummyPublisher : PublisherBase
    {
        public DummyPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var entry = report.Entries.FirstOrDefault(x => x.Key == this._healthCheck.Name);
            if (entry.Key == this._healthCheck.Name)
            {

            }
            return Task.CompletedTask;
        }

        protected internal override void Validate()
        {
            throw new System.NotImplementedException();
        }

        protected internal override void SetPublishing()
        {
            throw new System.NotImplementedException();
        }
    }
}
