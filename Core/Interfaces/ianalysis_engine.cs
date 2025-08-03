using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Analysis Engine Interface
    /// Responsibility: Define core analysis operations contract
    /// </summary>
    public interface IAnalysisEngine
    {
        /// <summary>
        /// Takes a system snapshot asynchronously
        /// </summary>
        /// <returns>System snapshot containing current system state</returns>
        Task<SystemSnapshot> TakeSnapshotAsync();

        /// <summary>
        /// Detects anomalies in the provided snapshot
        /// </summary>
        /// <param name="snapshot">System snapshot to analyze</param>
        /// <returns>Anomaly detection results</returns>
        Task<AnomalyDetectionResult> DetectAnomaliesAsync(SystemSnapshot snapshot);

        /// <summary>
        /// Gets analysis statistics from collected data
        /// </summary>
        /// <returns>Statistical analysis of collected snapshots</returns>
        AnalysisStatistics GetStatistics();
    }
}
