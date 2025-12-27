using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging.Console;
using NativeFluentConfig.Services;

namespace NativeFluentConfig;

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
            .ConfigureLogging((hostingContext, loggingBuilder) =>
            {
                loggingBuilder.AddDebug();
                loggingBuilder.AddEventLog();
                loggingBuilder.AddDefaultLogger();
                loggingBuilder.AddConsoleLogger();
                loggingBuilder.AddEventSourceLogger();
                loggingBuilder.AddFluentLoggingFilters();
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
                loggingBuilder.AddApplicationInsights(hostingContext);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddTransient<IDummyService, DummyService>();
            })
            .UseConsoleLifetime();

    private static void AddEventLog(this ILoggingBuilder loggingBuilder)
    {
#if (WINDOWS)
        loggingBuilder.AddEventLog();
#endif
    }
        
    private static void AddApplicationInsights(this ILoggingBuilder loggingBuilder, HostBuilderContext hostingContext)
    {
        const string key = "Logging:ApplicationInsights:ConnectionString";
        var connectionString = hostingContext.Configuration.GetValue<string>(key);
        loggingBuilder.AddApplicationInsights(
            config => config.ConnectionString = connectionString,
            options => options.IncludeScopes = true);
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

    private static void AddFluentLoggingFilters(this ILoggingBuilder loggingBuilder)
    {
        const string microsoft = @"Microsoft";

        loggingBuilder.AddFilter<ConsoleLoggerProvider>("*", LogLevel.Trace);
        loggingBuilder.AddFilter<ConsoleLoggerProvider>(microsoft, LogLevel.Information);

        loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("*", LogLevel.Information);
        loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>(microsoft, LogLevel.Warning);
    }

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