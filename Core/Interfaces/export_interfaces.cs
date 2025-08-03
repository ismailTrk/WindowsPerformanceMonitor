using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Data Exporter Interface
    /// Responsibility: Define export contract for different formats
    /// Pattern: Strategy Pattern
    /// </summary>
    public interface IDataExporter
    {
        ExportFormat SupportedFormat { get; }
        Task<string> ExportAsync(List<SystemSnapshot> snapshots, AnalysisStatistics statistics);
        Task SaveToFileAsync(string content, string filePath);
    }

    /// <summary>
    /// Exporter Factory Interface
    /// Responsibility: Create appropriate exporter based on format
    /// Pattern: Factory Pattern
    /// </summary>
    public interface IExporterFactory
    {
        IDataExporter GetExporter(ExportFormat format);
        IEnumerable<ExportFormat> GetSupportedFormats();
    }
}