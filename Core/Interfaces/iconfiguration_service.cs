using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Configuration Service Interface
    /// Responsibility: Handle application configuration
    /// Pattern: Single Responsibility Principle
    /// </summary>
    public interface IConfigurationService
    {
        AnalysisConfiguration LoadConfiguration(string[] args);
        void SaveConfiguration(AnalysisConfiguration config, string filePath);
        AnalysisConfiguration LoadFromFile(string filePath);
    }
}