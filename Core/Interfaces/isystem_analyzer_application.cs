namespace SystemAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Main Application Interface
    /// Responsibility: Define application contract
    /// Pattern: Interface Segregation Principle
    /// </summary>
    public interface ISystemAnalyzerApplication
    {
        Task RunAsync(string[] args);
    }
}