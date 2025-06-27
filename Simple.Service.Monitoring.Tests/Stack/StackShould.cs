using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Simple.Service.Monitoring.Tests.Stack
{
    [TestFixture(Category = "Functional")]
    public class StackShould
    {
        private IServiceCollection serviceCollection;
        private IConfiguration configuration;

        [SetUp]
        public void Setup()
        {

            var configurationbuilder = new ConfigurationBuilder();
            configuration = configurationbuilder.AddJsonFile("Stack/testsettings.json")
                .Build();
        }

        [Test]
        public void Given_Valid_Configuration_File_Stack_Should_Be_Initialized()
        {
            var observermock = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observermock.Setup(m => m.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback<KeyValuePair<string, HealthReportEntry>>((report) =>
                {
                    Assert.That(report.Value.Status == HealthStatus.Healthy);
                });

            var webhostBuilder = new WebHostBuilder()
                .Configure(z => z.Build())
                .ConfigureServices(services =>
                {
                    services.AddServiceMonitoring(configuration)
                        .WithApplicationSettings()
                        .WithAdditionalPublisherObserver(observermock.Object);
                });

            var server = new TestServer(webhostBuilder);
            Thread.Sleep(20000);
        }
    }
}
