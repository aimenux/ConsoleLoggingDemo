using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using SerilogFluentConfig.Services;

namespace SerilogFluentConfig;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        var service = host.Services.GetRequiredService<IDummyService>();
        await service.DoNothingAsync();
        Console.WriteLine("Press any key to exit !");
        Console.ReadKey();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((_, loggingBuilder) =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddDefaultLogger();
            })
            .UseSerilog((hostingContext, loggerConfiguration) =>
            {
                const string microsoft = @"Microsoft";
                const string logFile = @"c:\logs\serilog.log";
                const string key = "ApplicationInsights:ConnectionString";
                const string outputTemplate = @"[{Timestamp:HH:mm:ss} {Level:u3}] [{ThreadId}] [{SourceContext}] {Message:lj} {NewLine}{Exception}";

                SelfLog.Enable(Console.Error);
                
                var connectionString = hostingContext.Configuration.GetValue<string>(key);
                
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
                    .WriteTo.ApplicationInsights(connectionString, TelemetryConverter.Traces);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddTransient<IDummyService, DummyService>();
            })
            .UseConsoleLifetime();

    private static void AddDefaultLogger(this ILoggingBuilder loggingBuilder)
    {
        var categoryName = typeof(Program).Namespace!;
        var services = loggingBuilder.Services;
        services.AddSingleton(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger(categoryName);
        });
    }
}