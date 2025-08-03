namespace SystemAnalyzer.Core.Models
{
    /// <summary>
    /// Core Domain Models
    /// Responsibility: Define data structures and business entities
    /// Pattern: Value Object Pattern, Entity Pattern
    /// </summary>
    
    // Value Objects - Immutable data containers
    public record SystemSnapshot
    {
        public DateTime Timestamp { get; init; }
        public SystemInfo SystemInfo { get; init; }
        public List<ProcessInfo> Processes { get; init; } = new();
        public NetworkInfo NetworkInfo { get; init; }
    }

    public record SystemInfo
    {
        public double CpuUsage { get; init; }
        public double MemoryUsage { get; init; }
        public long TotalMemory { get; init; }
        public long AvailableMemory { get; init; }
        public double DiskUsage { get; init; }
        public int ProcessCount { get; init; }
        public TimeSpan SystemUptime { get; init; }
    }

    // Entity - Has identity (ProcessId)
    public record ProcessInfo
    {
        public int ProcessId { get; init; }
        public string ProcessName { get; init; } = string.Empty;
        public string ExecutablePath { get; init; } = string.Empty;
        public double CpuUsage { get; init; }
        public long MemoryUsage { get; init; }
        public int ThreadCount { get; init; }
        public DateTime StartTime { get; init; }
        public bool IsSystemProcess { get; init; }
        public int Priority { get; init; }
        public string UserName { get; init; } = string.Empty;
    }

    public record NetworkInfo
    {
        public long TotalBytesSent { get; init; }
        public long TotalBytesReceived { get; init; }
        public int ActiveConnections { get; init; }
        public List<NetworkConnection> Connections { get; init; } = new();
    }

    public record NetworkConnection
    {
        public string LocalEndpoint { get; init; } = string.Empty;
        public string RemoteEndpoint { get; init; } = string.Empty;
        public string Protocol { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public int ProcessId { get; init; }
    }

    // Configuration Object
    public record AnalysisConfiguration
    {
        public int IntervalSeconds { get; init; } = 60;
        public int Duration { get; init; } = 0;
        public string OutputPath { get; init; } = string.Empty;
        public ExportFormat Format { get; init; } = ExportFormat.Html;
        public bool Verbose { get; init; } = false;
        public bool SilentMode { get; init; } = false;
        public int CpuThreshold { get; init; } = 80;
        public int MemoryThreshold { get; init; } = 80;
        public bool NetworkMonitoring { get; init; } = true;
        public bool IsValid { get; init; } = true;
    }

    // Enums
    public enum ExportFormat
    {
        Html,
        Csv,
        Json,
        Txt
    }

    public enum UserCommandType
    {
        Quit,
        Pause,
        ShowStats,
        Clear,
        Help
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    // Command Pattern Models
    public record UserCommand
    {
        public UserCommandType Type { get; init; }
        public string[] Parameters { get; init; } = Array.Empty<string>();
    }

    // Statistics Value Object
    public record AnalysisStatistics
    {
        public int SnapshotCount { get; init; }
        public TimeSpan AnalysisDuration { get; init; }
        public double AverageCpuUsage { get; init; }
        public double MaximumCpuUsage { get; init; }
        public double MinimumCpuUsage { get; init; }
        public double AverageMemoryUsage { get; init; }
        public double MaximumMemoryUsage { get; init; }
        public double MinimumMemoryUsage { get; init; }
        public int TotalProcesses { get; init; }
    }

    // Anomaly Detection Result
    public record AnomalyDetectionResult
    {
        public bool HasAnomalies { get; init; }
        public List<string> Anomalies { get; init; } = new();
        public RiskLevel RiskLevel { get; init; }
    }
}