using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Monitors
{
    /// <summary>
    /// Windows Process Monitor Implementation
    /// Responsibility: Collect process information safely with exception handling
    /// Pattern: Repository Pattern, Safe Navigation Pattern
    /// </summary>
    public class WindowsProcessMonitor : IProcessMonitor
    {
        private readonly ILogger<WindowsProcessMonitor> _logger;
        private readonly HashSet<string> _systemProcessNames;

        public WindowsProcessMonitor(ILogger<WindowsProcessMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Define system processes for security analysis
            _systemProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System", "smss", "csrss", "wininit", "winlogon", "services", 
                "lsass", "svchost", "dwm", "explorer", "spoolsv", "audiodg"
            };
        }

        public async Task<List<ProcessInfo>> GetProcessesAsync()
        {
            try
            {
                _logger.LogDebug("Collecting process information...");

                var processes = Process.GetProcesses();
                var processInfoTasks = processes.Select(GetProcessInfoSafeAsync).ToArray();
                
                var processInfos = await Task.WhenAll(processInfoTasks);
                
                // Filter out null values and sort by CPU usage
                var validProcesses = processInfos
                    .Where(p => p != null)
                    .OrderByDescending(p => p.CpuUsage)
                    .ToList();

                // Dispose process array for memory management
                foreach (var process in processes)
                {
                    try { process.Dispose(); } catch { /* Ignore disposal errors */ }
                }

                _logger.LogDebug("Collected {Count} process entries", validProcesses.Count);
                return validProcesses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting process information");
                throw;
            }
        }

        private async Task<ProcessInfo> GetProcessInfoSafeAsync(Process process)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check if process still exists
                    if (process.HasExited)
                        return null;

                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = GetProcessNameSafe(process),
                        ExecutablePath = GetExecutablePathSafe(process),
                        CpuUsage = GetCpuUsageSafe(process),
                        MemoryUsage = GetMemoryUsageSafe(process),
                        ThreadCount = GetThreadCountSafe(process),
                        StartTime = GetStartTimeSafe(process),
                        Priority = GetPrioritySafe(process),
                        UserName = GetUserNameSafe(process),
                        IsSystemProcess = IsSystemProcess(process.ProcessName)
                    };

                    return processInfo;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not get info for process {ProcessId}", process?.Id);
                    return null;
                }
            });
        }

        private string GetProcessNameSafe(Process process)
        {
            try
            {
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetExecutablePathSafe(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? "Unknown";
            }
            catch
            {
                return "Access Denied";
            }
        }

        private double GetCpuUsageSafe(Process process)
        {
            try
            {
                // Simplified CPU calculation - in production, implement proper CPU monitoring
                // This would require performance counters or WMI queries
                return 0.0; // Placeholder
            }
            catch
            {
                return 0.0;
            }
        }

        private long GetMemoryUsageSafe(Process process)
        {
            try
            {
                return process.WorkingSet64;
            }
            catch
            {
                return 0;
            }
        }

        private int GetThreadCountSafe(Process process)
        {
            try
            {
                return process.Threads.Count;
            }
            catch
            {
                return 0;
            }
        }

        private DateTime GetStartTimeSafe(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private int GetPrioritySafe(Process process)
        {
            try
            {
                return process.BasePriority;
            }
            catch
            {
                return 0;
            }
        }

        private string GetUserNameSafe(Process process)
        {
            try
            {
                // WMI query required for user name - placeholder for now
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private bool IsSystemProcess(string processName)
        {
            return _systemProcessNames.Contains(processName);
        }
    }
}