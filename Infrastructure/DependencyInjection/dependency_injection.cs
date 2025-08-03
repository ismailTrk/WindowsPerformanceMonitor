using Microsoft.Extensions.DependencyInjection;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Services;
using SystemAnalyzer.Infrastructure.Monitors;
using SystemAnalyzer.Infrastructure.Exporters;
using SystemAnalyzer.Infrastructure.UI;
using SystemAnalyzer.Infrastructure.Security;

namespace SystemAnalyzer.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Dependency Injection Configuration Extensions
    /// Responsibility: Configure service container with proper lifetimes
    /// Pattern: Extension Methods, Service Locator Pattern
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register Core Business Logic Services
        /// </summary>
        public static IServiceCollection RegisterCoreServices(this IServiceCollection services)
        {
            // Application Services - Singleton for state management
            services.AddSingleton<ISystemAnalyzerApplication, SystemAnalyzerApplication>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IAnalysisEngine, AnalysisEngine>();
            services.AddSingleton<IReportGenerator, ReportGenerator>();
            
            // Security Services
            services.AddSingleton<IAnomalyDetector, AnomalyDetector>();
            
            return services;
        }

        /// <summary>
        /// Register Infrastructure Services (Data Access Layer)
        /// </summary>
        public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
        {
            // Monitor Services - Singleton for performance counters
            services.AddSingleton<ISystemMonitor, WindowsSystemMonitor>();
            services.AddSingleton<IProcessMonitor, WindowsProcessMonitor>();
            services.AddSingleton<INetworkMonitor, WindowsNetworkMonitor>();
            services.AddSingleton<IPerformanceCounter, WindowsPerformanceCounter>();
            
            return services;
        }

        /// <summary>
        /// Register Export Services (Presentation Layer)
        /// </summary>
        public static IServiceCollection RegisterExportServices(this IServiceCollection services)
        {
            // Export Services - Transient for stateless operations
            services.AddTransient<IDataExporter, HtmlExporter>();
            services.AddTransient<IDataExporter, CsvExporter>();
            services.AddTransient<IDataExporter, JsonExporter>();
            services.AddTransient<IDataExporter, TxtExporter>();
            
            // Factory - Singleton for service location
            services.AddSingleton<IExporterFactory, ExporterFactory>();
            
            return services;
        }

        /// <summary>
        /// Register UI Services
        /// </summary>
        public static IServiceCollection RegisterUIServices(this IServiceCollection services)
        {
            // UI Services - Singleton for state management
            services.AddSingleton<IConsoleDisplay, ConsoleDisplay>();
            services.AddSingleton<IUserInputHandler, UserInputHandler>();
            
            return services;
        }
    }
}