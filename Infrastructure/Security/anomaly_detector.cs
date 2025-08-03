using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Security
{
    public class AnomalyDetector : IAnomalyDetector
    {
        private readonly ILogger<AnomalyDetector> _logger;

        public AnomalyDetector(ILogger<AnomalyDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AnomalyDetectionResult> DetectAsync(SystemSnapshot snapshot, List<SystemSnapshot> history)
        {
            return await Task.Run(() =>
            {
                var anomalies = new List<string>();
                
                DetectCpuAnomalies(snapshot, anomalies);
                DetectMemoryAnomalies(snapshot, anomalies);
                DetectProcessAnomalies(snapshot, anomalies);
                DetectNetworkAnomalies(snapshot, anomalies);
                
                if (history.Count > 5)
                {
                    DetectHistoricalAnomalies(snapshot, history, anomalies);
                }

                var riskLevel = CalculateRiskLevel(anomalies, snapshot);
                
                if (anomalies.Any())
                {
                    _logger.LogWarning("Detected {Count} anomalies with risk level {RiskLevel}", 
                        anomalies.Count, riskLevel);
                }

                return new AnomalyDetectionResult
                {
                    HasAnomalies = anomalies.Any(),
                    Anomalies = anomalies,
                    RiskLevel = riskLevel
                };
            });
        }

        private void DetectCpuAnomalies(SystemSnapshot snapshot, List<string> anomalies)
        {
            if (snapshot.SystemInfo.CpuUsage > 95)
            {
                anomalies.Add($"Critical CPU usage: {snapshot.SystemInfo.CpuUsage:F1}%");
            }
            else if (snapshot.SystemInfo.CpuUsage > 80)
            {
                anomalies.Add($"High CPU usage: {snapshot.SystemInfo.CpuUsage:F1}%");
            }
        }

        private void DetectMemoryAnomalies(SystemSnapshot snapshot, List<string> anomalies)
        {
            if (snapshot.SystemInfo.MemoryUsage > 95)
            {
                anomalies.Add($"Critical memory usage: {snapshot.SystemInfo.MemoryUsage:F1}%");
            }
            else if (snapshot.SystemInfo.MemoryUsage > 80)
            {
                anomalies.Add($"High memory usage: {snapshot.SystemInfo.MemoryUsage:F1}%");
            }
        }

        private void DetectProcessAnomalies(SystemSnapshot snapshot, List<string> anomalies)
        {
            var suspiciousProcesses = snapshot.Processes
                .Where(p => !p.IsSystemProcess && p.CpuUsage > 50)
                .Where(p => string.IsNullOrEmpty(p.ExecutablePath) || 
                           p.ExecutablePath.ToLower().Contains("temp") ||
                           p.ExecutablePath.ToLower().Contains("appdata"))
                .ToList();

            foreach (var process in suspiciousProcesses)
            {
                anomalies.Add($"Suspicious process: {process.ProcessName} (CPU: {process.CpuUsage:F1}%)");
            }

            if (snapshot.Processes.Count > 300)
            {
                anomalies.Add($"Excessive process count: {snapshot.Processes.Count}");
            }
        }

        private void DetectNetworkAnomalies(SystemSnapshot snapshot, List<string> anomalies)
        {
            if (snapshot.NetworkInfo?.ActiveConnections > 1000)
            {
                anomalies.Add($"Excessive network connections: {snapshot.NetworkInfo.ActiveConnections}");
            }
        }

        private void DetectHistoricalAnomalies(SystemSnapshot current, List<SystemSnapshot> history, List<string> anomalies)
        {
            var recent = history.TakeLast(10).ToList();
            
            if (recent.Any())
            {
                var avgCpu = recent.Average(s => s.SystemInfo.CpuUsage);
                if (current.SystemInfo.CpuUsage > avgCpu * 2 && current.SystemInfo.CpuUsage > 50)
                {
                    anomalies.Add($"CPU spike detected: {current.SystemInfo.CpuUsage:F1}% (avg: {avgCpu:F1}%)");
                }

                var avgMemory = recent.Average(s => s.SystemInfo.MemoryUsage);
                if (current.SystemInfo.MemoryUsage > avgMemory * 1.5 && current.SystemInfo.MemoryUsage > 70)
                {
                    anomalies.Add($"Memory spike detected: {current.SystemInfo.MemoryUsage:F1}% (avg: {avgMemory:F1}%)");
                }
            }
        }

        private RiskLevel CalculateRiskLevel(List<string> anomalies, SystemSnapshot snapshot)
        {
            if (!anomalies.Any()) return RiskLevel.Low;

            var criticalCount = anomalies.Count(a => a.Contains("Critical") || a.Contains("Suspicious"));
            var warningCount = anomalies.Count(a => a.Contains("High"));

            if (criticalCount > 2 || 
                (snapshot.SystemInfo.CpuUsage > 95 && snapshot.SystemInfo.MemoryUsage > 95))
            {
                return RiskLevel.Critical;
            }

            if (criticalCount > 0 || warningCount > 3)
            {
                return RiskLevel.High;
            }

            if (warningCount > 0 || anomalies.Count > 2)
            {
                return RiskLevel.Medium;
            }

            return RiskLevel.Low;
        }
    }
}