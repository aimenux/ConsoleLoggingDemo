using System;
using System.Threading.Tasks;
using JsonSerilogThirdParty.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;

namespace JsonSerilogThirdParty;

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
                SelfLog.Enable(Console.Error);
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
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