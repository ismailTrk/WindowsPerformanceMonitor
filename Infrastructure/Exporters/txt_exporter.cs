using System.Text;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Exporters
{
    /// <summary>
    /// TXT Exporter Implementation
    /// Responsibility: Generate human-readable text reports
    /// Pattern: Template Method Pattern
    /// </summary>
    public class TxtExporter : IDataExporter
    {
        private readonly ILogger<TxtExporter> _logger;

        public ExportFormat SupportedFormat => ExportFormat.Txt;

        public TxtExporter(ILogger<TxtExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ExportAsync(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            try
            {
                _logger.LogInformation("Generating TXT report...");

                var report = await Task.Run(() => GenerateTextReport(snapshots, statistics));
                
                _logger.LogInformation("TXT report generated successfully");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating TXT report");
                throw;
            }
        }

        public async Task SaveToFileAsync(string content, string filePath)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                _logger.LogInformation("TXT report saved to: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving TXT file: {FilePath}", filePath);
                throw;
            }
        }

        private string GenerateTextReport(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            var report = new StringBuilder();

            // Header
            report.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            report.AppendLine("║                    SYSTEM PERFORMANCE ANALYSIS REPORT                       ║");
            report.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            report.AppendLine();

            // Report Information
            report.AppendLine("📋 REPORT INFORMATION");
            report.AppendLine(new string('─', 80));
            report.AppendLine($"Generated Date:       {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Analysis Duration:    {statistics.AnalysisDuration:hh\\:mm\\:ss}");
            report.AppendLine($"Total Snapshots:     {statistics.SnapshotCount}");
            report.AppendLine($"Report Format:        Text (TXT)");
            report.AppendLine();

            // System Statistics
            report.AppendLine("📊 SYSTEM PERFORMANCE STATISTICS");
            report.AppendLine(new string('─', 80));
            report.AppendLine($"CPU Usage:");
            report.AppendLine($"  ├─ Average:         {statistics.AverageCpuUsage:F2}%");
            report.AppendLine($"  ├─ Maximum:         {statistics.MaximumCpuUsage:F2}%");
            report.AppendLine($"  └─ Minimum:         {statistics.MinimumCpuUsage:F2}%");
            report.AppendLine();
            report.AppendLine($"Memory Usage:");
            report.AppendLine($"  ├─ Average:         {statistics.AverageMemoryUsage:F2}%");
            report.AppendLine($"  ├─ Maximum:         {statistics.MaximumMemoryUsage:F2}%");
            report.AppendLine($"  └─ Minimum:         {statistics.MinimumMemoryUsage:F2}%");
            report.AppendLine();
            report.AppendLine($"Process Information:");
            report.AppendLine($"  └─ Total Processes: {statistics.TotalProcesses}");
            report.AppendLine();

            // Current System Status
            if (snapshots.Any())
            {
                var latestSnapshot = snapshots.Last();
                report.AppendLine("🖥️ CURRENT SYSTEM STATUS");
                report.AppendLine(new string('─', 80));
                report.AppendLine($"Snapshot Time:        {latestSnapshot.Timestamp:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"CPU Usage:           {latestSnapshot.SystemInfo.CpuUsage:F1}% {GetStatusIndicator(latestSnapshot.SystemInfo.CpuUsage, 80)}");
                report.AppendLine($"Memory Usage:        {latestSnapshot.SystemInfo.MemoryUsage:F1}% {GetStatusIndicator(latestSnapshot.SystemInfo.MemoryUsage, 80)}");
                report.AppendLine($"Disk Usage:          {latestSnapshot.SystemInfo.DiskUsage:F1}%");
                report.AppendLine($"Active Processes:    {latestSnapshot.SystemInfo.ProcessCount}");
                report.AppendLine($"System Uptime:       {latestSnapshot.SystemInfo.SystemUptime:dd\\.hh\\:mm\\:ss}");
                
                if (latestSnapshot.NetworkInfo != null)
                {
                    var sentMB = latestSnapshot.NetworkInfo.TotalBytesSent / (1024.0 * 1024);
                    var receivedMB = latestSnapshot.NetworkInfo.TotalBytesReceived / (1024.0 * 1024);
                    report.AppendLine($"Network Connections: {latestSnapshot.NetworkInfo.ActiveConnections}");
                    report.AppendLine($"Data Sent:           {sentMB:F1} MB");
                    report.AppendLine($"Data Received:       {receivedMB:F1} MB");
                }
                report.AppendLine();

                // Top Processes
                report.AppendLine("🚀 TOP RESOURCE-CONSUMING PROCESSES");
                report.AppendLine(new string('─', 80));
                report.AppendLine($"{"PID",-8} {"Process Name",-25} {"CPU%",-8} {"RAM(MB)",-10} {"Threads",-8} {"Status",-10}");
                report.AppendLine(new string('─', 80));

                var topProcesses = latestSnapshot.Processes.Take(20);
                foreach (var process in topProcesses)
                {
                    var memoryMB = process.MemoryUsage / (1024 * 1024);
                    var processName = process.ProcessName.Length > 25 ? 
                        process.ProcessName.Substring(0, 22) + "..." : 
                        process.ProcessName;
                    var status = GetProcessStatus(process);

                    report.AppendLine($"{process.ProcessId,-8} {processName,-25} {process.CpuUsage,-8:F1} {memoryMB,-10:F0} {process.ThreadCount,-8} {status,-10}");
                }
                report.AppendLine();

                // System Alerts
                var alerts = GenerateSystemAlerts(latestSnapshot);
                if (alerts.Any())
                {
                    report.AppendLine("⚠️ SYSTEM ALERTS");
                    report.AppendLine(new string('─', 80));
                    foreach (var alert in alerts)
                    {
                        report.AppendLine($"  • {alert}");
                    }
                    report.AppendLine();
                }
            }

            // Performance Timeline (Last 10 snapshots)
            if (snapshots.Count > 1)
            {
                report.AppendLine("📈 PERFORMANCE TIMELINE (LAST 10 SNAPSHOTS)");
                report.AppendLine(new string('─', 80));
                report.AppendLine($"{"Time",-12} {"CPU%",-8} {"Memory%",-10} {"Processes",-10} {"Status",-15}");
                report.AppendLine(new string('─', 80));

                var recentSnapshots = snapshots.TakeLast(10);
                foreach (var snapshot in recentSnapshots)
                {
                    var timeStr = snapshot.Timestamp.ToString("HH:mm:ss");
                    var status = GetOverallStatus(snapshot.SystemInfo.CpuUsage, snapshot.SystemInfo.MemoryUsage);
                    
                    report.AppendLine($"{timeStr,-12} {snapshot.SystemInfo.CpuUsage,-8:F1} {snapshot.SystemInfo.MemoryUsage,-10:F1} {snapshot.SystemInfo.ProcessCount,-10} {status,-15}");
                }
                report.AppendLine();
            }

            // Recommendations
            if (snapshots.Any())
            {
                var recommendations = GenerateRecommendations(snapshots, statistics);
                if (recommendations.Any())
                {
                    report.AppendLine("💡 PERFORMANCE RECOMMENDATIONS");
                    report.AppendLine(new string('─', 80));
                    for (int i = 0; i < recommendations.Count; i++)
                    {
                        report.AppendLine($"{i + 1}. {recommendations[i]}");
                    }
                    report.AppendLine();
                }
            }

            // Footer
            report.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            report.AppendLine("║  Generated by System Analyzer v1.0 - Windows Performance Monitoring Tool   ║");
            report.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");

            return report.ToString();
        }

        private string GetStatusIndicator(double value, double threshold)
        {
            if (value > threshold) return "[HIGH]";
            if (value > threshold * 0.7) return "[MEDIUM]";
            return "[NORMAL]";
        }

        private string GetProcessStatus(ProcessInfo process)
        {
            if (process.IsSystemProcess) return "System";
            if (process.CpuUsage > 80) return "High CPU";
            if (process.CpuUsage > 50) return "Medium";
            return "Normal";
        }

        private string GetOverallStatus(double cpuUsage, double memoryUsage)
        {
            if (cpuUsage > 90 || memoryUsage > 90) return "CRITICAL";
            if (cpuUsage > 70 || memoryUsage > 70) return "WARNING";
            if (cpuUsage > 50 || memoryUsage > 50) return "ELEVATED";
            return "NORMAL";
        }

        private List<string> GenerateSystemAlerts(SystemSnapshot snapshot)
        {
            var alerts = new List<string>();

            if (snapshot.SystemInfo.CpuUsage > 90)
                alerts.Add($"Critical CPU usage detected: {snapshot.SystemInfo.CpuUsage:F1}%");

            if (snapshot.SystemInfo.MemoryUsage > 90)
                alerts.Add($"Critical memory usage detected: {snapshot.SystemInfo.MemoryUsage:F1}%");

            if (snapshot.SystemInfo.ProcessCount > 300)
                alerts.Add($"High number of processes: {snapshot.SystemInfo.ProcessCount}");

            var suspiciousProcesses = snapshot.Processes
                .Where(p => !p.IsSystemProcess && p.CpuUsage > 70)
                .Take(3);

            foreach (var process in suspiciousProcesses)
            {
                alerts.Add($"High CPU process detected: {process.ProcessName} ({process.CpuUsage:F1}%)");
            }

            if (snapshot.NetworkInfo?.ActiveConnections > 1000)
                alerts.Add($"High network activity: {snapshot.NetworkInfo.ActiveConnections} active connections");

            return alerts;
        }

        private List<string> GenerateRecommendations(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            var recommendations = new List<string>();

            if (statistics.AverageCpuUsage > 70)
            {
                recommendations.Add("Consider optimizing CPU-intensive applications or upgrading hardware.");
            }

            if (statistics.AverageMemoryUsage > 70)
            {
                recommendations.Add("Monitor memory usage and consider closing unnecessary applications.");
            }

            var latestSnapshot = snapshots.Last();
            var highCpuProcesses = latestSnapshot.Processes
                .Where(p => !p.IsSystemProcess && p.CpuUsage > 50)
                .Take(3);

            if (highCpuProcesses.Any())
            {
                var processNames = string.Join(", ", highCpuProcesses.Select(p => p.ProcessName));
                recommendations.Add($"Review high CPU processes: {processNames}");
            }

            if (latestSnapshot.SystemInfo.ProcessCount > 250)
            {
                recommendations.Add("Consider reviewing startup programs and background services.");
            }

            if (statistics.SnapshotCount > 10)
            {
                var cpuVariance = CalculateVariance(snapshots.Select(s => s.SystemInfo.CpuUsage).ToList());
                if (cpuVariance > 400) // High variance
                {
                    recommendations.Add("CPU usage shows high variability - investigate intermittent processes.");
                }
            }

            if (!recommendations.Any())
            {
                recommendations.Add("System performance appears to be within normal parameters.");
            }

            return recommendations;
        }

        private double CalculateVariance(List<double> values)
        {
            if (values.Count < 2) return 0;
            
            var mean = values.Average();
            var sumOfSquaredDifferences = values.Sum(x => Math.Pow(x - mean, 2));
            return sumOfSquaredDifferences / values.Count;
        }
    }
}