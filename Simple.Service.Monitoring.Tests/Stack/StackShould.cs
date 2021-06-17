using System;
using System.IO;
using System.Threading;
using Simple.Service.Monitoring.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;

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
            var observermock = new Mock<IObserver<HealthReport>>();
            observermock.Setup(m => m.OnNext(It.IsAny<HealthReport>()))
                .Callback<HealthReport>((report) =>
                {
                    Assert.That(report.Status == HealthStatus.Healthy);
                });

            var webhostBuilder = new WebHostBuilder()
                .Configure(z => z.Build())
                .ConfigureServices(services =>
                {
                    services.UseServiceMonitoring(configuration)
                        .UseSettings()
                        .AddPublisherObserver(observermock.Object);
                });

            var server = new TestServer(webhostBuilder);
            Thread.Sleep(20000);
        }
    }
}
