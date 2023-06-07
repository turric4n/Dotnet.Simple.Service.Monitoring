using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public abstract class GenericSqlWithCustomResultValidationMonitoringBase : ServiceMonitoringBase, IHealthCheckResultAnalyzer
    {
        protected string DEFAULTSQLQUERY = "SELECT 1;";

        protected GenericSqlWithCustomResultValidationMonitoringBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        public HealthCheckResult GetHealth(object healthCheckResultData)
        {
            if (healthCheckResultData == null) return HealthCheckResult.Unhealthy("No data returned from query");

            var expectedResult = HealthCheck.HealthCheckConditions.SqlBehaviour.ExpectedResult;
            var result = false;

            switch (HealthCheck.HealthCheckConditions.SqlBehaviour.ResultExpression)
            {
                case ResultExpression.Equal:
                    result = healthCheckResultData.Equals(expectedResult);
                    break;
                case ResultExpression.NotEqual:
                    result = !healthCheckResultData.Equals(expectedResult);
                    break;
                case ResultExpression.GreaterThan:
                    if (HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType == SqlResultDataType.Int ||
                        HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType == SqlResultDataType.DateTime)
                    {
                        result = (int)healthCheckResultData > (int)expectedResult;
                    }
                    break;
                case ResultExpression.LessThan:
                    if (HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType == SqlResultDataType.Int ||
                        HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType == SqlResultDataType.DateTime)
                    {
                        result = (int)healthCheckResultData < (int)expectedResult;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Result does not match expected result");
        }
    }
}
