using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Kythr.Library.Monitoring.Implementations
{
    public class CustomMonitoring : ServiceMonitoringBase, IDisposable
    {
        private readonly IHealthChecksBuilder _healthChecksBuilder;
        private readonly IServiceProvider _serviceProvider;
        private bool _disposed = false;

        public CustomMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _serviceProvider = _healthChecksBuilder.Services.BuildServiceProvider();
        }

        protected internal override void Validate()
        {
        }

        internal Func<Task<HealthCheckResult>> CustomCheck;

        public void AddCustomCheck(Func<Task<HealthCheckResult>> check)
        {
            CustomCheck = check;
        }

        internal static IEnumerable<Assembly> GetAssembliesFast()
        {
            var list = new List<string>();
            var stack = new Stack<Assembly>();

            stack.Push(Assembly.GetEntryAssembly());

            do
            {
                var asm = stack.Pop();

                yield return asm;

                foreach (var reference in asm.GetReferencedAssemblies().Where(name => !name.Name.Contains("System")))
                    if (!list.Contains(reference.FullName))
                    {
                        stack.Push(Assembly.Load(reference));
                        list.Add(reference.FullName);
                    }

            }
            while (stack.Count > 0);

        }

        protected internal override void SetMonitoring()
        {

            if (string.IsNullOrEmpty(this.HealthCheck.FullClassName))
            {
                HealthChecksBuilder.AddAsyncCheck(Name, () =>
                {
                    return CustomCheck != null ? CustomCheck() : Task.FromResult(HealthCheckResult.Healthy());
                });
            }

            else
            {
                Type classType = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assem in assemblies)
                {
                    classType = assem.GetType(HealthCheck.FullClassName);
                    if (classType != null) break;
                }

                if (classType == null)
                    throw new Exception(
                        $"Invalid class name {HealthCheck.FullClassName} defined in custom test named {Name}");

                var classInstance = ActivatorUtilities.CreateInstance(_serviceProvider, classType) as IHealthCheck;

                HealthChecksBuilder.AddAsyncCheck(
                    Name,
                    async () =>
                    {
                        return await classInstance.CheckHealthAsync(new HealthCheckContext(), System.Threading.CancellationToken.None);
                    });
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}
