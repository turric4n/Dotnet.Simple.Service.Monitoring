using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointMonitoringRouteBuilderExtensions
    {
        public static IApplicationBuilder AddServiceMonitoringEndpoints(this IApplicationBuilder builder)
        {
            //builder.UseEndpointRouting()

            return builder;
        }
    }
}
