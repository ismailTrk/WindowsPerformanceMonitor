using System.Text.Json;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Exporters
{
    /// <summary>
    /// JSON Exporter Implementation
    /// Responsibility: Generate JSON reports for programmatic access
    /// Pattern: Template Method Pattern, Serialization Pattern
    /// </summary>
    public class JsonExporter : IDataExporter
    {
        private readonly ILogger<JsonExporter> _logger;

        public ExportFormat SupportedFormat => ExportFormat.Json;

        public JsonExporter(ILogger<JsonExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ExportAsync(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            try
            {
                _logger.LogInformation("Generating JSON report...");

                var report = await Task.Run(() => GenerateJsonReport(snapshots, statistics));
                
                _logger.LogInformation("JSON report generated successfully");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JSON report");
                throw;
            }
        }

        public async Task SaveToFileAsync(string content, string filePath)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content);
                _logger.LogInformation("JSON report saved to: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving JSON file: {FilePath}", filePath);
                throw;
            }
        }

        private string GenerateJsonReport(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            var report = new
            {
                ReportMetadata = new
                {
                    GeneratedAt = DateTime.Now,
                    Version = "1.0",
                    Format = "JSON",
                    Description = "System Performance Analysis Report"
                },
                Statistics = new
                {
                    SnapshotCount = statistics.SnapshotCount,
                    AnalysisDuration = statistics.AnalysisDuration.ToString(@"hh\:mm\:ss"),
                    AnalysisDurationSeconds = statistics.AnalysisDuration.TotalSeconds,
                    CpuUsage = new
                    {
                        Average = Math.Round(statistics.AverageCpuUsage, 2),
                        Maximum = Math.Round(statistics.MaximumCpuUsage, 2),
                        Minimum = Math.Round(statistics.MinimumCpuUsage, 2)
                    },
                    MemoryUsage = new
                    {
                        Average = Math.Round(statistics.AverageMemoryUsage, 2),
                        Maximum = Math.Round(statistics.MaximumMemoryUsage, 2),
                        Minimum = Math.Round(statistics.MinimumMemoryUsage, 2)
                    },
                    TotalProcesses = statistics.TotalProcesses
                },
                SystemSnapshots = snapshots.Select(s => new
                {
                    Timestamp = s.Timestamp,
                    TimestampUnix = ((DateTimeOffset)s.Timestamp).ToUnixTimeSeconds(),
                    SystemInfo = new
                    {
                        CpuUsage = Math.Round(s.SystemInfo.CpuUsage, 2),
                        MemoryUsage = Math.Round(s.SystemInfo.MemoryUsage, 2),
                        DiskUsage = Math.Round(s.SystemInfo.DiskUsage, 2),
                        TotalMemory = s.SystemInfo.TotalMemory,
                        AvailableMemory = s.SystemInfo.AvailableMemory,
                        ProcessCount = s.SystemInfo.ProcessCount,
                        SystemUptimeSeconds = s.SystemInfo.SystemUptime.TotalSeconds
                    },
                    TopProcesses = s.Processes.Take(20).Select(p => new
                    {
                        ProcessId = p.ProcessId,
                        ProcessName = p.ProcessName,
                        ExecutablePath = p.ExecutablePath,
                        CpuUsage = Math.Round(p.CpuUsage, 2),
                        MemoryUsageMB = Math.Round(p.MemoryUsage / (1024.0 * 1024), 2),
                        MemoryUsageBytes = p.MemoryUsage,
                        ThreadCount = p.ThreadCount,
                        StartTime = p.StartTime,
                        IsSystemProcess = p.IsSystemProcess,
                        Priority = p.Priority,
                        UserName = p.UserName
                    }),
                    NetworkInfo = s.NetworkInfo != null ? new
                    {
                        TotalBytesSent = s.NetworkInfo.TotalBytesSent,
                        TotalBytesReceived = s.NetworkInfo.TotalBytesReceived,
                        TotalBytesSentMB = Math.Round(s.NetworkInfo.TotalBytesSent / (1024.0 * 1024), 2),
                        TotalBytesReceivedMB = Math.Round(s.NetworkInfo.TotalBytesReceived / (1024.0 * 1024), 2),
                        ActiveConnections = s.NetworkInfo.ActiveConnections,
                        Connections = s.NetworkInfo.Connections.Take(10).Select(c => new
                        {
                            LocalEndpoint = c.LocalEndpoint,
                            RemoteEndpoint = c.RemoteEndpoint,
                            Protocol = c.Protocol,
                            State = c.State,
                            ProcessId = c.ProcessId
                        })
                    } : null
                }).ToList(),
                Summary = new
                {
                    TotalSnapshots = snapshots.Count,
                    AnalysisStartTime = snapshots.FirstOrDefault()?.Timestamp,
                    AnalysisEndTime = snapshots.LastOrDefault()?.Timestamp,
                    HighestCpuUsage = snapshots.Any() ? Math.Round(snapshots.Max(s => s.SystemInfo.CpuUsage), 2) : 0,
                    HighestMemoryUsage = snapshots.Any() ? Math.Round(snapshots.Max(s => s.SystemInfo.MemoryUsage), 2) : 0,
                    AverageProcessCount = snapshots.Any() ? Math.Round(snapshots.Average(s => s.SystemInfo.ProcessCount), 0) : 0
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(report, options);
        }
    }
}