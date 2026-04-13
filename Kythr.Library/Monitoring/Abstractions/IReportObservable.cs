using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Kythr.Library.Monitoring.Abstractions
{
    public interface IReportObservable : IObservable<HealthReport>
    {
    }
}
