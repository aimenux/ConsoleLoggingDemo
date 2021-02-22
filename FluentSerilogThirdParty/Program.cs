using System;
using System.IO;
using System.Threading.Tasks;
using FluentSerilogThirdParty.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace FluentSerilogThirdParty
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using (var host = CreateHostBuilder(args).Build())
            {
                var service = host.Services.GetRequiredService<IDummyService>();
                await service.DoNothingAsync();
            }

            Console.WriteLine("Press any key to exit !");
            Console.ReadKey();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddCommandLine(args);
                    config.AddEnvironmentVariables();
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging((_, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddNonGenericLogger();
                })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    const string microsoft = @"Microsoft";
                    const string logFile = @"c:\logs\serilog.log";
                    const string key = "ApplicationInsights:InstrumentationKey";
                    const string outputTemplate = @"[{Timestamp:HH:mm:ss} {Level:u3}] [{ThreadId}] [{SourceContext}] {Message:lj} {NewLine}{Exception}";

                    var instrumentationKey = hostingContext.Configuration.GetValue<string>(key);

                    SelfLog.Enable(Console.Error);
                    loggerConfiguration
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override(microsoft, LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .Enrich.WithThreadId()
                        .Enrich.WithThreadName()
                        .Enrich.WithProcessName()
                        .Enrich.WithMachineName()
                        .Enrich.WithEnvironmentUserName()
                        .WriteTo.Console(outputTemplate: outputTemplate)
                        .WriteTo.File(logFile, rollingInterval:RollingInterval.Day)
                        .WriteTo.ApplicationInsights(instrumentationKey, TelemetryConverter.Traces);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddOptions();
                    services.AddTransient<IDummyService, DummyService>();
                })
                .UseConsoleLifetime();

        private static void AddNonGenericLogger(this ILoggingBuilder loggingBuilder)
        {
            var categoryName = typeof(Program).Namespace;
            var services = loggingBuilder.Services;
            services.AddSingleton(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger(categoryName);
            });
        }
    }
}
