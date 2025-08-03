using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Console Display Interface
    /// Responsibility: Define console output contract
    /// Pattern: Single Responsibility Principle
    /// </summary>
    public interface IConsoleDisplay
    {
        void ShowHeader(AnalysisConfiguration config);
        void ShowHelp();
        void DisplaySnapshot(SystemSnapshot snapshot, AnalysisConfiguration config);
        void ShowStatistics(AnalysisStatistics statistics);
        void ShowMessage(string message);
        void Clear();
    }

    /// <summary>
    /// User Input Handler Interface
    /// Responsibility: Define user input processing contract
    /// Pattern: Command Pattern
    /// </summary>
    public interface IUserInputHandler
    {
        Task StartMonitoringAsync(Action<UserCommand> commandHandler, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Report Generator Interface
    /// Responsibility: Define report generation contract
    /// Pattern: Strategy Pattern
    /// </summary>
    public interface IReportGenerator
    {
        Task AddSnapshotAsync(SystemSnapshot snapshot);
        Task GenerateFinalReportAsync();
        Task<string> GenerateReportAsync(ExportFormat format);
        void SetOutputConfiguration(string outputPath, ExportFormat format);
        int GetSnapshotCount();
        void ClearSnapshots();
    }
}