using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public interface IReportObservable : IObservable<HealthReport>
    {
    }
}
