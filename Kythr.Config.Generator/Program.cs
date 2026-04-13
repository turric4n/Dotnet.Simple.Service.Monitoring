using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using Newtonsoft.Json;
using Kythr.Config.Generator.Infrastructure;
using Kythr.Config.Generator.Infrastructure.Json;
using Kythr.Config.Generator.Infrastructure.Yaml;
using Kythr.Library.Options;
using YamlDotNet.Serialization;
using Kythr.Config.Generator.Config;

namespace Kythr.Config.Generator
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();

            ConfigureServices(services);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.File(@".\Log.txt")
                .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();

                builder.AddSerilog(logger);

                logger.Information("Start");
            });

            services.AddTransient<Func<ExtensionType, IConfigManipulator<MonitoringWrapper>>>(serviceProvider => key =>
            {
                try
                {
                    switch (key)
                    {
                        case ExtensionType.Yaml:
                            return serviceProvider.GetRequiredService<YmlConfigManipulator<MonitoringWrapper>>();
                        case ExtensionType.Json:
                            return serviceProvider.GetRequiredService<JsonConfigManipulator<MonitoringWrapper>>();
                        default:
                            throw new ArgumentOutOfRangeException(nameof(key), key, null);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            });

            services.AddTransient<IExtensionValidatorService, ExtensionValidatorService>();

            services.AddSingleton<YmlConfigManipulator<MonitoringWrapper>>();
            services.AddSingleton<JsonConfigManipulator<MonitoringWrapper>>();

            services.AddScoped<MainForm>();

            services.AddTransient<HealthCheckForm>();

            services.BuildServiceProvider();
        }
    }
}