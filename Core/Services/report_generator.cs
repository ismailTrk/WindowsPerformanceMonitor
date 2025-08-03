using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;
using System.Diagnostics;

namespace SystemAnalyzer.Core.Services
{
    /// <summary>
    /// Report Generator Implementation
    /// Responsibility: Manage snapshot collection and final report generation
    /// Pattern: Facade Pattern, Template Method Pattern
    /// </summary>
    public class ReportGenerator : IReportGenerator
    {
        private readonly IExporterFactory _exporterFactory;
        private readonly IAnalysisEngine _analysisEngine;
        private readonly ILogger<ReportGenerator> _logger;
        
        private readonly List<SystemSnapshot> _snapshots;
        private readonly object _lockObject = new object();
        private readonly Stopwatch _performanceTimer;
        
        private string _outputPath = string.Empty;
        private ExportFormat _format = ExportFormat.Html;
        private volatile bool _isConfigured = false;

        public ReportGenerator(
            IExporterFactory exporterFactory,
            IAnalysisEngine analysisEngine,
            ILogger<ReportGenerator> logger)
        {
            _exporterFactory = exporterFactory ?? throw new ArgumentNullException(nameof(exporterFactory));
            _analysisEngine = analysisEngine ?? throw new ArgumentNullException(nameof(analysisEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _snapshots = new List<SystemSnapshot>();
            _performanceTimer = Stopwatch.StartNew();
            
            _logger.LogDebug("ReportGenerator initialized");
        }

        public async Task AddSnapshotAsync(SystemSnapshot snapshot)
        {
            if (snapshot == null)
            {
                _logger.LogWarning("Attempted to add null snapshot");
                return;
            }

            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    _snapshots.Add(snapshot);
                    
                    // Memory management - keep last 5000 snapshots (configurable)
                    const int maxSnapshots = 5000;
                    if (_snapshots.Count > maxSnapshots)
                    {
                        var removeCount = _snapshots.Count - maxSnapshots;
                        _snapshots.RemoveRange(0, removeCount);
                        
                        _logger.LogDebug("Removed {Count} old snapshots for memory management", removeCount);
                    }
                }
                
                _logger.LogTrace("Snapshot added. Total: {Count}, Memory: {Memory:F2}MB", 
                    _snapshots.Count, GC.GetTotalMemory(false) / (1024.0 * 1024));
            });
        }

        public async Task GenerateFinalReportAsync()
        {
            var timer = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Generating final report...");
                
                if (!_isConfigured)
                {
                    _logger.LogWarning("Report generator not configured. Using default settings.");
                    SetDefaultConfiguration();
                }

                var statistics = _analysisEngine.GetStatistics();
                
                _logger.LogDebug("Report statistics: {SnapshotCount} snapshots, {Duration} analysis duration",
                    statistics.SnapshotCount, statistics.AnalysisDuration);

                var report = await GenerateReportAsync(_format);
                
                if (string.IsNullOrEmpty(_outputPath))
                {
                    _logger.LogError("Output path is not configured");
                    throw new InvalidOperationException("Output path must be configured before generating report");
                }
                
                var exporter = _exporterFactory.GetExporter(_format);
                await exporter.SaveToFileAsync(report, _outputPath);
                
                var fullPath = Path.GetFullPath(_outputPath);
                var fileSize = new FileInfo(fullPath).Length;
                
                timer.Stop();
                
                _logger.LogInformation("Report generated successfully: {FilePath} ({Size:F2}KB) in {Duration:F2}s", 
                    fullPath, fileSize / 1024.0, timer.Elapsed.TotalSeconds);
                    
                Console.WriteLine($"\n✅ Report saved: {fullPath}");
                Console.WriteLine($"📊 File size: {fileSize / 1024.0:F2} KB");
                Console.WriteLine($"⏱️  Generation time: {timer.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (Exception ex)
            {
                timer.Stop();
                _logger.LogError(ex, "Error generating final report after {Duration:F2}s", timer.Elapsed.TotalSeconds);
                throw;
            }
        }

        public async Task<string> GenerateReportAsync(ExportFormat format)
        {
            var timer = Stopwatch.StartNew();
            
            try
            {
                var exporter = _exporterFactory.GetExporter(format);
                var statistics = _analysisEngine.GetStatistics();
                
                List<SystemSnapshot> snapshotsCopy;
                lock (_lockObject)
                {
                    snapshotsCopy = new List<SystemSnapshot>(_snapshots);
                }
                
                _logger.LogDebug("Generating {Format} report with {Count} snapshots", format, snapshotsCopy.Count);
                
                if (!snapshotsCopy.Any())
                {
                    _logger.LogWarning("No snapshots available for report generation");
                    return GenerateEmptyReport(format);
                }
                
                var report = await exporter.ExportAsync(snapshotsCopy, statistics);
                
                timer.Stop();
                
                _logger.LogDebug("Report generated: {Format}, {Length} chars, {Duration:F2}s", 
                    format, report.Length, timer.Elapsed.TotalSeconds);
                
                return report;
            }
            catch (Exception ex)
            {
                timer.Stop();
                _logger.LogError(ex, "Error generating {Format} report after {Duration:F2}s", format, timer.Elapsed.TotalSeconds);
                throw;
            }
        }

        public void SetOutputConfiguration(string outputPath, ExportFormat format)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
            }

            _outputPath = outputPath;
            _format = format;
            _isConfigured = true;
            
            _logger.LogInformation("Output configuration set: {OutputPath}, Format: {Format}", outputPath, format);
        }

        public int GetSnapshotCount()
        {
            lock (_lockObject)
            {
                return _snapshots.Count;
            }
        }

        public void ClearSnapshots()
        {
            lock (_lockObject)
            {
                var count = _snapshots.Count;
                _snapshots.Clear();
                _logger.LogInformation("Cleared {Count} snapshots from memory", count);
            }
            
            // Force garbage collection to free memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void SetDefaultConfiguration()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _outputPath = $"system_analysis_{timestamp}.html";
            _format = ExportFormat.Html;
            _isConfigured = true;
            
            _logger.LogInformation("Using default configuration: {OutputPath}, Format: {Format}", _outputPath, _format);
        }

        private string GenerateEmptyReport(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Html => GenerateEmptyHtmlReport(),
                ExportFormat.Json => "{ \"error\": \"No data available\", \"snapshots\": [] }",
                ExportFormat.Csv => "# No data available for analysis",
                ExportFormat.Txt => "System Analysis Report\n\nNo data available for analysis.",
                _ => "No data available for analysis."
            };
        }

        private string GenerateEmptyHtmlReport()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>System Analysis Report - No Data</title>
    <style>
        body { font-family: 'Segoe UI', Arial, sans-serif; text-align: center; padding: 50px; background: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #e74c3c; margin-bottom: 20px; }
        p { color: #7f8c8d; line-height: 1.6; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>⚠️ System Analysis Report</h1>
        <p>No performance data was collected during the analysis period.</p>
        <p>This might be due to a short analysis duration or system monitoring issues.</p>
        <p>Generated on: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + @"</p>
    </div>
</body>
</html>";
        }
    }
}