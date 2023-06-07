﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    public class CustomMonitoringShould
    {
        private IHealthChecksBuilder healthChecksBuilder;
        [SetUp]
        public void Setup()
        {
            var healthcheckbuildermock = new Moq.Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            healthChecksBuilder = healthcheckbuildermock.Object;
        }

        [Test]
        public void Given_Valid_Custom_Monitoring_Will_Return_Healthy_Status()
        {
            //Arrange
            var custompointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Custom
            };
        }
    }
}
