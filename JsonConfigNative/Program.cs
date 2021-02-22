using System;
using System.IO;
using System.Threading.Tasks;
using JsonConfigNative.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace JsonConfigNative
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
                .ConfigureLogging((hostingContext, loggingBuilder) =>
                {
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddEventLog();
                    loggingBuilder.AddConsoleLogger();
                    loggingBuilder.AddNonGenericLogger();
                    loggingBuilder.AddEventSourceLogger();
                    loggingBuilder.AddApplicationInsights(GetInstrumentationKey(hostingContext));
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddOptions();
                    services.AddTransient<IDummyService, DummyService>();
                })
                .UseConsoleLifetime();

        private static string GetInstrumentationKey(HostBuilderContext hostingContext)
        {
            const string key = "Logging:ApplicationInsights:InstrumentationKey";
            var instrumentationKey = hostingContext.Configuration.GetValue<string>(key);
            return instrumentationKey;
        }

        private static void AddConsoleLogger(this ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "[HH:mm:ss:fff] ";
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });
        }

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
