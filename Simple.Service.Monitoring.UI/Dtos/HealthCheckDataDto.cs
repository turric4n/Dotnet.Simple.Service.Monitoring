using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Dtos
{
    public class HealthCheckDataDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MachineName { get; set; }
        public string ServiceType { get; set; }
        public int Status { get; set; }
        public string Duration { get; set; }
        public string Description { get; set; }
        public string CheckError { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
