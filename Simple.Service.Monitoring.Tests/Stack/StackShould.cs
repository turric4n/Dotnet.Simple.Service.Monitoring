using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Tests.Stack
{
    [TestFixture(Category = "Functional")]
    public class StackShould
    {
        private IConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            var configurationbuilder = new ConfigurationBuilder();
            configuration = configurationbuilder.AddJsonFile("Stack/testsettings.json")
                .Build();
        }

        [Test]
        public async Task Given_Valid_Configuration_File_Stack_Should_Be_Initialized()
        {
            var observermock = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observermock.Setup(m => m.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback<KeyValuePair<string, HealthReportEntry>>((report) =>
                {
                    Assert.That(report.Value.Status == HealthStatus.Healthy);
                });

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddServiceMonitoring(configuration)
                        .WithApplicationSettings()
                        .WithAdditionalPublisherObserver(observermock.Object);
                });

            var host = await hostBuilder.StartAsync();
            
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
            finally
            {
                await host.StopAsync();
                host.Dispose();
            }
        }
    }
}
