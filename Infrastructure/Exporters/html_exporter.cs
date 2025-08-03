using System.Text;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Exporters
{
    /// <summary>
    /// HTML Exporter Implementation
    /// Responsibility: Generate rich HTML reports with high readability
    /// Pattern: Template Method Pattern, Builder Pattern
    /// </summary>
    public class HtmlExporter : IDataExporter
    {
        private readonly ILogger<HtmlExporter> _logger;

        public ExportFormat SupportedFormat => ExportFormat.Html;

        public HtmlExporter(ILogger<HtmlExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ExportAsync(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            try
            {
                _logger.LogInformation("Generating HTML report...");

                if (!snapshots.Any())
                {
                    return GenerateEmptyReport();
                }

                var html = await Task.Run(() => GenerateHtmlReport(snapshots, statistics));
                
                _logger.LogInformation("HTML report generated successfully");
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HTML report");
                throw;
            }
        }

        public async Task SaveToFileAsync(string content, string filePath)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                _logger.LogInformation("HTML report saved to: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving HTML file: {FilePath}", filePath);
                throw;
            }
        }

        private string GenerateHtmlReport(List<SystemSnapshot> snapshots, AnalysisStatistics statistics)
        {
            var html = new StringBuilder();
            var latestSnapshot = snapshots.LastOrDefault();

            html.AppendLine(GenerateHtmlHeader());
            html.AppendLine(GenerateReportHeader(statistics));
            html.AppendLine(GenerateSystemSummary(latestSnapshot, statistics));
            html.AppendLine(GenerateProcessTable(latestSnapshot));
            html.AppendLine(GenerateNetworkSection(latestSnapshot));
            html.AppendLine(GenerateTimelineSection(snapshots));
            html.AppendLine(GenerateFooter());

            return html.ToString();
        }

        private string GenerateEmptyReport()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>System Analysis Report - No Data</title>
    <style>body { font-family: Arial; text-align: center; padding: 50px; }</style>
</head>
<body>
    <h1>System Analysis Report</h1>
    <p>No data available for analysis.</p>
</body>
</html>";
        }

        private string GenerateHtmlHeader()
        {
            return @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>System Performance Analysis Report</title>
    <style>
        :root {
            --primary-color: #2c3e50;
            --secondary-color: #3498db;
            --success-color: #27ae60;
            --warning-color: #f39c12;
            --danger-color: #e74c3c;
            --background-color: #ecf0f1;
            --card-background: #ffffff;
            --text-color: #2c3e50;
            --border-color: #bdc3c7;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: var(--background-color);
            color: var(--text-color);
            line-height: 1.6;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 20px;
        }

        .header {
            background: linear-gradient(135deg, var(--primary-color) 0%, var(--secondary-color) 100%);
            color: white;
            padding: 40px 30px;
            border-radius: 15px;
            margin-bottom: 30px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.1);
            text-align: center;
        }

        .header h1 {
            font-size: 3rem;
            font-weight: 300;
            margin-bottom: 10px;
        }

        .header .subtitle {
            opacity: 0.9;
            font-size: 1.1rem;
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 25px;
            margin-bottom: 40px;
        }

        .stat-card {
            background: var(--card-background);
            padding: 30px;
            border-radius: 15px;
            box-shadow: 0 5px 20px rgba(0,0,0,0.08);
            border-left: 5px solid var(--secondary-color);
            transition: transform 0.3s ease;
        }

        .stat-card:hover {
            transform: translateY(-5px);
        }

        .stat-card.warning {
            border-left-color: var(--warning-color);
        }

        .stat-card.danger {
            border-left-color: var(--danger-color);
        }

        .stat-card h3 {
            font-size: 1.1rem;
            margin-bottom: 15px;
            color: var(--text-color);
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .stat-card .value {
            font-size: 2.5rem;
            font-weight: bold;
            color: var(--secondary-color);
            margin-bottom: 10px;
        }

        .stat-card.warning .value {
            color: var(--warning-color);
        }

        .stat-card.danger .value {
            color: var(--danger-color);
        }

        .progress-bar {
            background: #e9ecef;
            border-radius: 10px;
            overflow: hidden;
            height: 12px;
            margin-top: 10px;
        }

        .progress-fill {
            height: 100%;
            border-radius: 10px;
            transition: width 0.6s ease;
        }

        .progress-cpu {
            background: linear-gradient(90deg, var(--success-color), var(--warning-color), var(--danger-color));
        }

        .progress-memory {
            background: linear-gradient(90deg, #3498db, var(--warning-color), var(--danger-color));
        }

        .section {
            background: var(--card-background);
            margin: 30px 0;
            border-radius: 15px;
            overflow: hidden;
            box-shadow: 0 5px 20px rgba(0,0,0,0.08);
        }

        .section-header {
            background: var(--primary-color);
            color: white;
            padding: 20px 30px;
            font-size: 1.4rem;
            font-weight: 500;
        }

        .section-content {
            padding: 30px;
        }

        .table-responsive {
            overflow-x: auto;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }

        th, td {
            padding: 15px 12px;
            text-align: left;
            border-bottom: 1px solid var(--border-color);
        }

        th {
            background-color: #f8f9fa;
            font-weight: 600;
            color: var(--primary-color);
            position: sticky;
            top: 0;
        }

        tr:hover {
            background-color: #f8f9fa;
        }

        .high-usage {
            color: var(--danger-color);
            font-weight: bold;
        }

        .medium-usage {
            color: var(--warning-color);
            font-weight: bold;
        }

        .low-usage {
            color: var(--success-color);
        }

        .system-process {
            background-color: #e3f2fd;
        }

        .suspicious-process {
            background-color: #ffebee;
            border-left: 3px solid var(--danger-color);
        }

        .status-badge {
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 0.85rem;
            font-weight: 500;
        }

        .status-normal {
            background-color: #d4edda;
            color: #155724;
        }

        .status-warning {
            background-color: #fff3cd;
            color: #856404;
        }

        .status-danger {
            background-color: #f8d7da;
            color: #721c24;
        }

        .footer {
            text-align: center;
            margin-top: 50px;
            padding: 30px;
            color: #7f8c8d;
            font-size: 0.9rem;
        }

        @media (max-width: 768px) {
            .container {
                padding: 10px;
            }
            
            .header h1 {
                font-size: 2rem;
            }
            
            .stats-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>";
        }

        private string GenerateReportHeader(AnalysisStatistics statistics)
        {
            return $@"
<div class='container'>
    <div class='header'>
        <h1>🖥️ System Performance Analysis</h1>
        <div class='subtitle'>
            <p>📅 Analysis Date: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
            <p>⏱️ Analysis Duration: {statistics.AnalysisDuration:hh\:mm\:ss}</p>
            <p>📊 Total Snapshots: {statistics.SnapshotCount}</p>
        </div>
    </div>";
        }

        private string GenerateSystemSummary(SystemSnapshot latestSnapshot, AnalysisStatistics statistics)
        {
            if (latestSnapshot?.SystemInfo == null) return "";

            var cpuClass = GetStatusClass(latestSnapshot.SystemInfo.CpuUsage, 70, 90);
            var memoryClass = GetStatusClass(latestSnapshot.SystemInfo.MemoryUsage, 70, 90);

            return $@"
    <div class='stats-grid'>
        <div class='stat-card {cpuClass}'>
            <h3>💻 CPU Usage</h3>
            <div class='value'>{latestSnapshot.SystemInfo.CpuUsage:F1}%</div>
            <div class='progress-bar'>
                <div class='progress-fill progress-cpu' style='width: {latestSnapshot.SystemInfo.CpuUsage}%'></div>
            </div>
            <small>Avg: {statistics.AverageCpuUsage:F1}% | Max: {statistics.MaximumCpuUsage:F1}%</small>
        </div>

        <div class='stat-card {memoryClass}'>
            <h3>🧠 Memory Usage</h3>
            <div class='value'>{latestSnapshot.SystemInfo.MemoryUsage:F1}%</div>
            <div class='progress-bar'>
                <div class='progress-fill progress-memory' style='width: {latestSnapshot.SystemInfo.MemoryUsage}%'></div>
            </div>
            <small>Avg: {statistics.AverageMemoryUsage:F1}% | Max: {statistics.MaximumMemoryUsage:F1}%</small>
        </div>

        <div class='stat-card'>
            <h3>💾 Disk Usage</h3>
            <div class='value'>{latestSnapshot.SystemInfo.DiskUsage:F1}%</div>
            <small>I/O Performance Monitor</small>
        </div>

        <div class='stat-card'>
            <h3>🔧 System Info</h3>
            <div class='value'>{latestSnapshot.SystemInfo.ProcessCount}</div>
            <small>Active Processes</small>
            <br><small>Uptime: {latestSnapshot.SystemInfo.SystemUptime:dd\.hh\:mm}</small>
        </div>
    </div>";
        }

        private string GenerateProcessTable(SystemSnapshot latestSnapshot)
        {
            if (latestSnapshot?.Processes == null || !latestSnapshot.Processes.Any())
            {
                return @"
    <div class='section'>
        <div class='section-header'>🚀 Process Analysis</div>
        <div class='section-content'>No process data available.</div>
    </div>";
            }

            var tableRows = new StringBuilder();
            var topProcesses = latestSnapshot.Processes.Take(25);

            foreach (var process in topProcesses)
            {
                var cpuClass = GetUsageClass(process.CpuUsage, 30, 70);
                var rowClass = process.IsSystemProcess ? "system-process" : 
                              (process.CpuUsage > 80 && !process.IsSystemProcess) ? "suspicious-process" : "";
                
                var statusBadge = GetProcessStatusBadge(process);
                var memoryMB = process.MemoryUsage / (1024 * 1024);
                var fileName = string.IsNullOrEmpty(process.ExecutablePath) ? "Unknown" : 
                              Path.GetFileName(process.ExecutablePath);

                tableRows.AppendLine($@"
                <tr class='{rowClass}'>
                    <td>{process.ProcessId}</td>
                    <td><strong>{process.ProcessName}</strong></td>
                    <td class='{cpuClass}'>{process.CpuUsage:F1}%</td>
                    <td>{memoryMB:F0} MB</td>
                    <td>{process.ThreadCount}</td>
                    <td>{process.StartTime:HH:mm:ss}</td>
                    <td title='{process.ExecutablePath}'>{fileName}</td>
                    <td>{statusBadge}</td>
                </tr>");
            }

            return $@"
    <div class='section'>
        <div class='section-header'>🚀 Top Resource-Consuming Processes</div>
        <div class='section-content'>
            <div class='table-responsive'>
                <table>
                    <thead>
                        <tr>
                            <th>PID</th>
                            <th>Process Name</th>
                            <th>CPU %</th>
                            <th>Memory</th>
                            <th>Threads</th>
                            <th>Start Time</th>
                            <th>Executable</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        {tableRows}
                    </tbody>
                </table>
            </div>
        </div>
    </div>";
        }

        private string GenerateNetworkSection(SystemSnapshot latestSnapshot)
        {
            if (latestSnapshot?.NetworkInfo == null)
            {
                return @"
    <div class='section'>
        <div class='section-header'>🌐 Network Information</div>
        <div class='section-content'>Network monitoring not available.</div>
    </div>";
            }

            var networkInfo = latestSnapshot.NetworkInfo;
            var sentMB = networkInfo.TotalBytesSent / (1024.0 * 1024);
            var receivedMB = networkInfo.TotalBytesReceived / (1024.0 * 1024);

            return $@"
    <div class='section'>
        <div class='section-header'>🌐 Network Information</div>
        <div class='section-content'>
            <div class='stats-grid'>
                <div class='stat-card'>
                    <h3>📤 Data Sent</h3>
                    <div class='value'>{sentMB:F1} MB</div>
                </div>
                <div class='stat-card'>
                    <h3>📥 Data Received</h3>
                    <div class='value'>{receivedMB:F1} MB</div>
                </div>
                <div class='stat-card'>
                    <h3>🔗 Active Connections</h3>
                    <div class='value'>{networkInfo.ActiveConnections}</div>
                </div>
            </div>
        </div>
    </div>";
        }

        private string GenerateTimelineSection(List<SystemSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return "";

            var timelineData = snapshots.TakeLast(50).Select(s => new
            {
                Time = s.Timestamp.ToString("HH:mm:ss"),
                Cpu = s.SystemInfo.CpuUsage,
                Memory = s.SystemInfo.MemoryUsage
            });

            return @"
    <div class='section'>
        <div class='section-header'>📈 Performance Timeline (Last 50 Snapshots)</div>
        <div class='section-content'>
            <p>Performance trends over time would be displayed here with a chart library like Chart.js</p>
            <p>This section shows CPU and Memory usage patterns to identify performance trends.</p>
        </div>
    </div>";
        }

        private string GenerateFooter()
        {
            return $@"
    <div class='footer'>
        <p>Generated by System Analyzer v1.0</p>
        <p>Report created on {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
        <p>For more information about system performance optimization, consult your system administrator.</p>
    </div>
</div>
</body>
</html>";
        }

        private string GetStatusClass(double value, double warningThreshold, double dangerThreshold)
        {
            if (value > dangerThreshold) return "danger";
            if (value > warningThreshold) return "warning";
            return "";
        }

        private string GetUsageClass(double value, double mediumThreshold, double highThreshold)
        {
            if (value > highThreshold) return "high-usage";
            if (value > mediumThreshold) return "medium-usage";
            return "low-usage";
        }

        private string GetProcessStatusBadge(ProcessInfo process)
        {
            if (process.IsSystemProcess)
                return "<span class='status-badge status-normal'>🔧 System</span>";
            
            if (process.CpuUsage > 80)
                return "<span class='status-badge status-danger'>⚠️ High</span>";
            
            if (process.CpuUsage > 50)
                return "<span class='status-badge status-warning'>📈 Medium</span>";
            
            return "<span class='status-badge status-normal'>✅ Normal</span>";
        }
    }
}