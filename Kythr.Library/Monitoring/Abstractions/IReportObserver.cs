using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Kythr.Library.Monitoring.Abstractions
{
    public interface IReportObserver : IObserver<HealthReport>
    {
        public bool ExecuteAlways { get; }
    }
}
