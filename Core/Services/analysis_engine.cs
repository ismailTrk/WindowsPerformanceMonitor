using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Core.Services
{
    /// <summary>
    /// Analysis Engine Implementation
    /// Responsibility: Coordinate system monitoring and perform analysis
    /// Pattern: Facade Pattern, Observer Pattern
    /// </summary>
    public class AnalysisEngine : IAnalysisEngine
    {
        private readonly ISystemMonitor _systemMonitor;
        private readonly IProcessMonitor _processMonitor;
        private readonly INetworkMonitor _networkMonitor;
        private readonly IAnomalyDetector _anomalyDetector;
        private readonly ILogger<AnalysisEngine> _logger;
        private readonly List<SystemSnapshot> _snapshotHistory;
        private readonly object _lockObject = new object();

        public AnalysisEngine(
            ISystemMonitor systemMonitor,
            IProcessMonitor processMonitor,
            INetworkMonitor networkMonitor,
            IAnomalyDetector anomalyDetector,
            ILogger<AnalysisEngine> logger)
        {
            _systemMonitor = systemMonitor ?? throw new ArgumentNullException(nameof(systemMonitor));
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            _anomalyDetector = anomalyDetector ?? throw new ArgumentNullException(nameof(anomalyDetector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _snapshotHistory = new List<SystemSnapshot>();
        }

        /// <summary>
        /// Takes a comprehensive system snapshot
        /// </summary>
        /// <returns>System snapshot containing current system state</returns>
        public async Task<SystemSnapshot> TakeSnapshotAsync()
        {
            try
            {
                _logger.LogDebug("Taking system snapshot...");

                // Parallel execution for better performance
                var systemInfoTask = _systemMonitor.GetSystemInfoAsync();
                var processesTask = _processMonitor.GetProcessesAsync();
                var networkInfoTask = _networkMonitor.GetNetworkInfoAsync();

                await Task.WhenAll(systemInfoTask, processesTask, networkInfoTask);

                var snapshot = new SystemSnapshot
                {
                    Timestamp = DateTime.Now,
                    SystemInfo = await systemInfoTask,
                    Processes = await processesTask,
                    NetworkInfo = await networkInfoTask
                };

                // Thread-safe snapshot history management
                lock (_lockObject)
                {
                    _snapshotHistory.Add(snapshot);
                    
                    // Memory management - keep last 1000 snapshots
                    if (_snapshotHistory.Count > 1000)
                    {
                        _snapshotHistory.RemoveAt(0);
                    }
                }

                // Perform anomaly detection on the new snapshot
                await DetectAnomaliesAsync(snapshot);

                _logger.LogDebug("Snapshot taken successfully. Total snapshots: {Count}", _snapshotHistory.Count);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking system snapshot");
                throw;
            }
        }

        /// <summary>
        /// Detects anomalies in the provided snapshot
        /// </summary>
        /// <param name="snapshot">System snapshot to analyze</param>
        /// <returns>Anomaly detection results</returns>
        public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(SystemSnapshot snapshot)
        {
            try
            {
                _logger.LogDebug("Detecting anomalies for snapshot at {Timestamp}", snapshot.Timestamp);

                List<SystemSnapshot> historyCopy;
                lock (_lockObject)
                {
                    historyCopy = new List<SystemSnapshot>(_snapshotHistory);
                }

                var result = await _anomalyDetector.DetectAsync(snapshot, historyCopy);

                if (result.HasAnomalies)
                {
                    _logger.LogWarning("Anomalies detected: {Count} anomalies found", result.Anomalies.Count);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting anomalies");
                throw;
            }
        }

        /// <summary>
        /// Gets comprehensive analysis statistics from collected data
        /// </summary>
        /// <returns>Statistical analysis of collected snapshots</returns>
        public AnalysisStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                if (!_snapshotHistory.Any())
                {
                    _logger.LogDebug("No snapshots available for statistics");
                    return new AnalysisStatistics
                    {
                        SnapshotCount = 0,
                        AnalysisDuration = TimeSpan.Zero,
                        AverageCpuUsage = 0,
                        MaximumCpuUsage = 0,
                        MinimumCpuUsage = 0,
                        AverageMemoryUsage = 0,
                        MaximumMemoryUsage = 0,
                        MinimumMemoryUsage = 0,
                        TotalProcesses = 0
                    };
                }

                try
                {
                    var cpuValues = _snapshotHistory.Select(s => s.SystemInfo.CpuUsage).ToList();
                    var memoryValues = _snapshotHistory.Select(s => s.SystemInfo.MemoryUsage).ToList();
                    var firstSnapshot = _snapshotHistory.First();
                    var lastSnapshot = _snapshotHistory.Last();

                    var statistics = new AnalysisStatistics
                    {
                        SnapshotCount = _snapshotHistory.Count,
                        AnalysisDuration = lastSnapshot.Timestamp - firstSnapshot.Timestamp,
                        AverageCpuUsage = cpuValues.Any() ? cpuValues.Average() : 0,
                        MaximumCpuUsage = cpuValues.Any() ? cpuValues.Max() : 0,
                        MinimumCpuUsage = cpuValues.Any() ? cpuValues.Min() : 0,
                        AverageMemoryUsage = memoryValues.Any() ? memoryValues.Average() : 0,
                        MaximumMemoryUsage = memoryValues.Any() ? memoryValues.Max() : 0,
                        MinimumMemoryUsage = memoryValues.Any() ? memoryValues.Min() : 0,
                        TotalProcesses = lastSnapshot.Processes.Count
                    };

                    _logger.LogDebug("Statistics calculated for {Count} snapshots", _snapshotHistory.Count);

                    return statistics;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating statistics");
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the current snapshot count
        /// </summary>
        /// <returns>Number of snapshots in history</returns>
        public int GetSnapshotCount()
        {
            lock (_lockObject)
            {
                return _snapshotHistory.Count;
            }
        }

        /// <summary>
        /// Clears all snapshot history (use with caution)
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                var count = _snapshotHistory.Count;
                _snapshotHistory.Clear();
                _logger.LogInformation("Cleared {Count} snapshots from history", count);
            }
        }
    }
}
