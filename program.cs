using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Services;
using SystemAnalyzer.Infrastructure.DependencyInjection;

namespace SystemAnalyzer
{
    /// <summary>
    /// Application Entry Point
    /// Responsibility: Bootstrap application and configure DI container
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var app = host.Services.GetRequiredService<ISystemAnalyzerApplication>();
            await app.RunAsync(args);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.RegisterCoreServices();
                    services.RegisterInfrastructureServices();
                    services.RegisterExportServices();
                    services.RegisterUIServices();
                    
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                });
    }
}