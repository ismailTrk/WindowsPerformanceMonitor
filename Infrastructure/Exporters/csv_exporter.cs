using System.Text;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Exporters
{
    /// <summary>
    /// CSV Exporter Implementation
    /// Responsibility: Generate CSV reports for data analysis
    /// Pattern: Template Method Pattern
    /// </summary>
    public class CsvExporter : IDataExporter
    {
        private readonly ILogger<CsvExporter> _logger;

        public ExportFormat SupportedFormat => ExportFormat.Csv;

        public CsvExporter(ILogger<CsvExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ExportAsync(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            try
            {
                _logger.LogInformation("Generating CSV report...");

                var csv = await Task.Run(() => GenerateCsvReport(snapshots, statistics));
                
                _logger.LogInformation("CSV report generated successfully");
                return csv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV report");
                throw;
            }
        }

        public async Task SaveToFileAsync(string content, string filePath)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                _logger.LogInformation("CSV report saved to: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving CSV file: {FilePath}", filePath);
                throw;
            }
        }

        private string GenerateCsvReport(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            var csv = new StringBuilder();

            // Summary Information
            csv.AppendLine("# System Analysis Summary");
            csv.AppendLine($"Analysis Date,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Analysis Duration,{statistics.AnalysisDuration:hh\\:mm\\:ss}");
            csv.AppendLine($"Total Snapshots,{statistics.SnapshotCount}");
            csv.AppendLine($"Average CPU Usage,{statistics.AverageCpuUsage:F2}");
            csv.AppendLine($"Maximum CPU Usage,{statistics.MaximumCpuUsage:F2}");
            csv.AppendLine($"Average Memory Usage,{statistics.AverageMemoryUsage:F2}");
            csv.AppendLine($"Maximum Memory Usage,{statistics.MaximumMemoryUsage:F2}");
            csv.AppendLine();

            // System Performance Data
            csv.AppendLine("# System Performance Timeline");
            csv.AppendLine("Timestamp,CPU Usage %,Memory Usage %,Disk Usage %,Process Count,Active Connections");
            
            foreach (var snapshot in snapshots)
            {
                csv.AppendLine($"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"{snapshot.SystemInfo.CpuUsage:F2}," +
                              $"{snapshot.SystemInfo.MemoryUsage:F2}," +
                              $"{snapshot.SystemInfo.DiskUsage:F2}," +
                              $"{snapshot.SystemInfo.ProcessCount}," +
                              $"{snapshot.NetworkInfo?.ActiveConnections ?? 0}");
            }

            csv.AppendLine();

            // Process Information (from latest snapshot)
            if (snapshots.Any())
            {
                var latestSnapshot = snapshots.Last();
                csv.AppendLine("# Process Information (Latest Snapshot)");
                csv.AppendLine("PID,Process Name,CPU Usage %,Memory Usage MB,Thread Count,Start Time,Executable Path,Is System Process");
                
                foreach (var process in latestSnapshot.Processes.Take(50)) // Top 50 processes
                {
                    var memoryMB = process.MemoryUsage / (1024 * 1024);
                    var processName = EscapeCsvField(process.ProcessName);
                    var executablePath = EscapeCsvField(process.ExecutablePath);
                    
                    csv.AppendLine($"{process.ProcessId}," +
                                  $"{processName}," +
                                  $"{process.CpuUsage:F2}," +
                                  $"{memoryMB:F0}," +
                                  $"{process.ThreadCount}," +
                                  $"{process.StartTime:yyyy-MM-dd HH:mm:ss}," +
                                  $"{executablePath}," +
                                  $"{process.IsSystemProcess}");
                }
            }

            return csv.ToString();
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Escape quotes and wrap in quotes if necessary
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}