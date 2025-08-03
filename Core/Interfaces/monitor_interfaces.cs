using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Core.Interfaces
{
    /// <summary>
    /// System Monitor Interface
    /// Responsibility: Define system monitoring contract
    /// Pattern: Interface Segregation Principle
    /// </summary>
    public interface ISystemMonitor
    {
        Task<SystemInfo> GetSystemInfoAsync();
    }

    /// <summary>
    /// Process Monitor Interface
    /// Responsibility: Define process monitoring contract
    /// Pattern: Interface Segregation Principle
    /// </summary>
    public interface IProcessMonitor
    {
        Task<List<ProcessInfo>> GetProcessesAsync();
    }

    /// <summary>
    /// Network Monitor Interface
    /// Responsibility: Define network monitoring contract
    /// Pattern: Interface Segregation Principle
    /// </summary>
    public interface INetworkMonitor
    {
        Task<NetworkInfo> GetNetworkInfoAsync();
    }

    /// <summary>
    /// Performance Counter Interface
    /// Responsibility: Abstract Windows Performance Counters
    /// Pattern: Adapter Pattern
    /// </summary>
    public interface IPerformanceCounter
    {
        Task<double> GetCpuUsageAsync();
        Task<double> GetMemoryUsageAsync();
        Task<double> GetDiskUsageAsync();
    }

    /// <summary>
    /// Anomaly Detector Interface
    /// Responsibility: Define anomaly detection contract
    /// Pattern: Strategy Pattern
    /// </summary>
    public interface IAnomalyDetector
    {
        Task<AnomalyDetectionResult> DetectAsync(SystemSnapshot snapshot, List<SystemSnapshot> history);
    }
}