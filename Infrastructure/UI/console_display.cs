using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.UI
{
    /// <summary>
    /// Console Display Implementation
    /// Responsibility: Handle all console output formatting and display
    /// Pattern: Facade Pattern for console operations
    /// </summary>
    public class ConsoleDisplay : IConsoleDisplay
    {
        private readonly ILogger<ConsoleDisplay> _logger;
        private readonly object _consoleLock = new object();
        private DateTime _lastMessageTime = DateTime.MinValue;

        public ConsoleDisplay(ILogger<ConsoleDisplay> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ShowHeader(AnalysisConfiguration config)
        {
            lock (_consoleLock)
            {
                try
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║              SYSTEM ANALYZER - PERFORMANCE MONITOR          ║");
                    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                    Console.ResetColor();
                    
                    Console.WriteLine($"📅 Start Time: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                    Console.WriteLine($"⏱️  Interval: {config.IntervalSeconds} seconds");
                    Console.WriteLine($"⏰ Duration: {(config.Duration > 0 ? config.Duration + " minutes" : "Unlimited")}");
                    Console.WriteLine($"📄 Output: {config.OutputPath} ({config.Format.ToString().ToUpper()})");
                    Console.WriteLine($"🚨 CPU Threshold: {config.CpuThreshold}% | Memory Threshold: {config.MemoryThreshold}%");
                    Console.WriteLine(new string('─', 64));
                    Console.WriteLine("Commands: [Q]uit, [P]ause, [S]tats, [C]lear, [H]elp");
                    Console.WriteLine(new string('─', 64));
                    
                    _logger.LogDebug("Header displayed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error displaying header");
                }
            }
        }

        public void ShowHelp()
        {
            lock (_consoleLock)
            {
                try
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("🔧 SYSTEM ANALYZER - HELP");
                    Console.ResetColor();
                    Console.WriteLine(new string('═', 60));
                    
                    Console.WriteLine("\n📖 USAGE:");
                    Console.WriteLine("  SystemAnalyzer.exe [options]");
                    
                    Console.WriteLine("\n⚙️  OPTIONS:");
                    Console.WriteLine("  -i, --interval <seconds>     Monitoring interval (default: 60)");
                    Console.WriteLine("  -d, --duration <minutes>     Analysis duration (default: unlimited)");
                    Console.WriteLine("  -o, --output <path>          Output file path");
                    Console.WriteLine("  -f, --format <format>        Output format: html, csv, json, txt");
                    Console.WriteLine("  -v, --verbose               Verbose output");
                    Console.WriteLine("  -s, --silent                Silent mode");
                    Console.WriteLine("  -t, --threshold-cpu <n>     CPU warning threshold % (default: 80)");
                    Console.WriteLine("  -m, --threshold-mem <n>     Memory warning threshold % (default: 80)");
                    Console.WriteLine("  -h, --help                  Show this help");
                    
                    Console.WriteLine("\n💡 EXAMPLES:");
                    Console.WriteLine("  SystemAnalyzer.exe -i 30 -d 10 -f html");
                    Console.WriteLine("  SystemAnalyzer.exe --interval 120 --output report.html --verbose");
                    Console.WriteLine("  SystemAnalyzer.exe -t 70 -m 85 -f json -o performance.json");
                    
                    Console.WriteLine("\n🎮 RUNTIME COMMANDS:");
                    Console.WriteLine("  Q - Quit application");
                    Console.WriteLine("  P - Pause/Resume monitoring");
                    Console.WriteLine("  S - Show statistics");
                    Console.WriteLine("  C - Clear screen");
                    Console.WriteLine("  H - Show this help");
                    
                    Console.WriteLine(new string('═', 60));
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error displaying help");
                }
            }
        }

        public void DisplaySnapshot(SystemSnapshot snapshot, AnalysisConfiguration config)
        {
            if (snapshot?.SystemInfo == null) return;

            lock (_consoleLock)
            {
                try
                {
                    // Move to data display area (below header)
                    Console.SetCursorPosition(0, 12);
                    
                    // System Information with color coding
                    DisplaySystemInfo(snapshot.SystemInfo, config);
                    
                    // Network information
                    DisplayNetworkInfo(snapshot.NetworkInfo);
                    
                    Console.WriteLine(new string('─', 64));
                    Console.WriteLine("🚀 Top CPU-Consuming Processes:");
                    Console.WriteLine($"{"PID",-8} {"Name",-20} {"CPU%",-8} {"RAM(MB)",-10} {"Threads",-8}");
                    Console.WriteLine(new string('─', 64));
                    
                    // Display top processes
                    DisplayTopProcesses(snapshot.Processes, config);
                    
                    // Clear remaining lines to prevent artifacts
                    ClearRemainingLines();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error displaying snapshot");
                }
            }
        }

        public void ShowStatistics(AnalysisStatistics statistics)
        {
            lock (_consoleLock)
            {
                try
                {
                    Console.WriteLine("\n" + new string('═', 60));
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("📊 ANALYSIS STATISTICS");
                    Console.ResetColor();
                    Console.WriteLine(new string('═', 60));
                    
                    Console.WriteLine($"⏱️  Analysis Duration: {statistics.AnalysisDuration:hh\\:mm\\:ss}");
                    Console.WriteLine($"📸 Snapshots Taken: {statistics.SnapshotCount:N0}");
                    
                    Console.WriteLine($"\n💻 CPU Statistics:");
                    Console.WriteLine($"  ├─ Average: {statistics.AverageCpuUsage:F1}%");
                    Console.WriteLine($"  ├─ Maximum: {statistics.MaximumCpuUsage:F1}%");
                    Console.WriteLine($"  └─ Minimum: {statistics.MinimumCpuUsage:F1}%");
                    
                    Console.WriteLine($"\n🧠 Memory Statistics:");
                    Console.WriteLine($"  ├─ Average: {statistics.AverageMemoryUsage:F1}%");
                    Console.WriteLine($"  ├─ Maximum: {statistics.MaximumMemoryUsage:F1}%");
                    Console.WriteLine($"  └─ Minimum: {statistics.MinimumMemoryUsage:F1}%");
                    
                    Console.WriteLine($"\n🔢 Process Count: {statistics.TotalProcesses:N0}");
                    
                    // Performance indicators
                    ShowPerformanceIndicators(statistics);
                    
                    Console.WriteLine(new string('═', 60));
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error displaying statistics");
                }
            }
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            lock (_consoleLock)
            {
                try
                {
                    // Rate limiting for messages
                    var now = DateTime.Now;
                    if (now - _lastMessageTime < TimeSpan.FromMilliseconds(500))
                        return;

                    _lastMessageTime = now;

                    var currentPos = GetConsoleCursorPositionSafe();
                    
                    // Move to message area (bottom of console)
                    var messageRow = Math.Max(0, Console.WindowHeight - 3);
                    Console.SetCursorPosition(0, messageRow);
                    
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"ℹ️  {message}".PadRight(Console.WindowWidth - 1));
                    Console.ResetColor();
                    
                    // Restore cursor position if valid
                    if (currentPos.Left >= 0 && currentPos.Top >= 0)
                    {
                        Console.SetCursorPosition(currentPos.Left, currentPos.Top);
                    }
                    
                    // Auto-clear message after delay
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        ClearMessageArea();
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error displaying message: {Message}", message);
                }
            }
        }

        public void Clear()
        {
            lock (_consoleLock)
            {
                try
                {
                    Console.Clear();
                    _logger.LogDebug("Console cleared");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing console");
                }
            }
        }

        private void DisplaySystemInfo(SystemInfo systemInfo, AnalysisConfiguration config)
        {
            // CPU Usage with color coding
            Console.ForegroundColor = GetColorForValue(systemInfo.CpuUsage, config.CpuThreshold);
            Console.WriteLine($"💻 CPU: {systemInfo.CpuUsage:F1}%".PadRight(25));
            
            // Memory Usage with color coding
            Console.ForegroundColor = GetColorForValue(systemInfo.MemoryUsage, config.MemoryThreshold);
            Console.WriteLine($"🧠 RAM: {systemInfo.MemoryUsage:F1}%".PadRight(25));
            
            // Disk Usage
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"💾 Disk: {systemInfo.DiskUsage:F1}%".PadRight(25));
            
            Console.ResetColor();
            Console.WriteLine($"⏰ Time: {DateTime.Now:HH:mm:ss}".PadRight(25));
            Console.WriteLine($"🔢 Processes: {systemInfo.ProcessCount}".PadRight(25));
            Console.WriteLine($"⏳ Uptime: {systemInfo.SystemUptime:dd\\.hh\\:mm}".PadRight(25));
        }

        private void DisplayNetworkInfo(NetworkInfo? networkInfo)
        {
            if (networkInfo != null)
            {
                var sentMB = networkInfo.TotalBytesSent / (1024.0 * 1024);
                var receivedMB = networkInfo.TotalBytesReceived / (1024.0 * 1024);
                
                Console.WriteLine($"🌐 Network: {networkInfo.ActiveConnections} connections".PadRight(25));
                Console.WriteLine($"📤 Sent: {sentMB:F1} MB | 📥 Received: {receivedMB:F1} MB".PadRight(25));
            }
        }

        private void DisplayTopProcesses(List<ProcessInfo> processes, AnalysisConfiguration config)
        {
            var topProcesses = processes.Take(10);
            
            foreach (var process in topProcesses)
            {
                Console.ForegroundColor = GetProcessColor(process, config);
                
                var ramMB = process.MemoryUsage / (1024 * 1024);
                var nameShort = TruncateString(process.ProcessName, 20);
                
                var statusIcon = GetProcessStatusIcon(process);
                
                Console.WriteLine($"{process.ProcessId,-8} {nameShort,-20} " +
                                $"{process.CpuUsage,-8:F1} {ramMB,-10:F0} {process.ThreadCount,-8} {statusIcon}");
            }
            
            Console.ResetColor();
        }

        private void ShowPerformanceIndicators(AnalysisStatistics statistics)
        {
            Console.WriteLine("\n🎯 Performance Indicators:");
            
            // CPU Health
            var cpuHealth = GetHealthStatus(statistics.AverageCpuUsage, 50, 80);
            Console.WriteLine($"  ├─ CPU Health: {GetHealthIcon(cpuHealth)} {cpuHealth}");
            
            // Memory Health  
            var memoryHealth = GetHealthStatus(statistics.AverageMemoryUsage, 60, 80);
            Console.WriteLine($"  ├─ Memory Health: {GetHealthIcon(memoryHealth)} {memoryHealth}");
            
            // Overall System Health
            var overallHealth = GetOverallHealth(statistics);
            Console.WriteLine($"  └─ Overall Health: {GetHealthIcon(overallHealth)} {overallHealth}");
        }

        private ConsoleColor GetColorForValue(double value, double threshold)
        {
            if (value > threshold) return ConsoleColor.Red;
            if (value > threshold * 0.7) return ConsoleColor.Yellow;
            return ConsoleColor.Green;
        }

        private ConsoleColor GetProcessColor(ProcessInfo process, AnalysisConfiguration config)
        {
            if (process.CpuUsage > config.CpuThreshold) return ConsoleColor.Red;
            if (process.CpuUsage > config.CpuThreshold * 0.5) return ConsoleColor.Yellow;
            if (process.IsSystemProcess) return ConsoleColor.Cyan;
            return ConsoleColor.White;
        }

        private string GetProcessStatusIcon(ProcessInfo process)
        {
            if (process.IsSystemProcess) return "🔧";
            if (process.CpuUsage > 80) return "🔥";
            if (process.CpuUsage > 50) return "⚡";
            return "✅";
        }

        private string GetHealthStatus(double value, double goodThreshold, double warningThreshold)
        {
            if (value <= goodThreshold) return "Excellent";
            if (value <= warningThreshold) return "Good";
            if (value <= 90) return "Warning";
            return "Critical";
        }

        private string GetHealthIcon(string health)
        {
            return health switch
            {
                "Excellent" => "🟢",
                "Good" => "🟡",
                "Warning" => "🟠",
                "Critical" => "🔴",
                _ => "⚪"
            };
        }

        private string GetOverallHealth(AnalysisStatistics statistics)
        {
            var avgCpu = statistics.AverageCpuUsage;
            var avgMemory = statistics.AverageMemoryUsage;
            var maxCpu = statistics.MaximumCpuUsage;
            var maxMemory = statistics.MaximumMemoryUsage;

            if (maxCpu > 95 || maxMemory > 95) return "Critical";
            if (avgCpu > 80 || avgMemory > 80) return "Warning";
            if (avgCpu > 60 || avgMemory > 70) return "Good";
            return "Excellent";
        }

        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (input.Length <= maxLength) return input;
            return input.Substring(0, maxLength - 3) + "...";
        }

        private (int Left, int Top) GetConsoleCursorPositionSafe()
        {
            try
            {
                return (Console.CursorLeft, Console.CursorTop);
            }
            catch
            {
                return (-1, -1);
            }
        }

        private void ClearRemainingLines()
        {
            try
            {
                var currentLine = Console.CursorTop;
                var windowHeight = Console.WindowHeight;
                
                // Clear remaining lines to prevent display artifacts
                for (int i = currentLine; i < Math.Min(windowHeight - 2, currentLine + 5); i++)
                {
                    Console.WriteLine(new string(' ', Console.WindowWidth - 1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error clearing remaining lines");
            }
        }

        private void ClearMessageArea()
        {
            lock (_consoleLock)
            {
                try
                {
                    var messageRow = Math.Max(0, Console.WindowHeight - 3);
                    Console.SetCursorPosition(0, messageRow);
                    Console.WriteLine(new string(' ', Console.WindowWidth - 1));
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Error clearing message area");
                }
            }
        }
    }
}
                    