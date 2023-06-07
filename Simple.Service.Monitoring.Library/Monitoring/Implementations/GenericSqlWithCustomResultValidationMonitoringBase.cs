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

            var targetType = GetTargetType(HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType);

            var expectedResult =
                Convert.ChangeType(HealthCheck.HealthCheckConditions.SqlBehaviour.ExpectedResult, targetType);
            
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
                        result = (long)healthCheckResultData > (long)expectedResult;
                    }
                    break;
                case ResultExpression.LessThan:
                    if (HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType == SqlResultDataType.Int ||
                        HealthCheck.HealthCheckConditions.SqlBehaviour.SqlResultDataType == SqlResultDataType.DateTime)
                    {
                        result = (long)healthCheckResultData < (long)expectedResult;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Result does not match expected result");
        }

        internal Type GetTargetType(SqlResultDataType dataType)
        {
            return dataType switch
            {
                SqlResultDataType.String => typeof(string),
                SqlResultDataType.Int => typeof(long),
                SqlResultDataType.Bool => typeof(bool),
                SqlResultDataType.DateTime => typeof(DateTime),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
            };
        }
    }
}
