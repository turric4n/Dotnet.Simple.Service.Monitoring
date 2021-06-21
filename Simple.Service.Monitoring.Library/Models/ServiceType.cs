using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Models
{
    public enum ServiceType
    {
        Custom,
        HttpEndpoint,
        ElasticSearch,
        Sql,
        Rmq,
        Hangfire,
        Ping
    }
}
