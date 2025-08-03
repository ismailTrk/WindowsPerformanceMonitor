using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Monitors
{
    public class WindowsProcessMonitor : IProcessMonitor
    {
        private readonly ILogger<WindowsProcessMonitor> _logger;
        private readonly HashSet<string> _systemProcessNames;
        private readonly Dictionary<int, ProcessCpuInfo> _processCpuHistory;
        private readonly object _lockObject = new object();

        public WindowsProcessMonitor(ILogger<WindowsProcessMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _systemProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System", "smss", "csrss", "wininit", "winlogon", "services", 
                "lsass", "svchost", "dwm", "explorer", "spoolsv", "audiodg",
                "conhost", "dllhost", "rundll32", "taskeng", "taskhostw",
                "SearchIndexer", "WmiPrvSE", "msdtc", "lsm"
            };
            
            _processCpuHistory = new Dictionary<int, ProcessCpuInfo>();
        }

        public async Task<List<ProcessInfo>> GetProcessesAsync()
        {
            try
            {
                _logger.LogDebug("Collecting process information...");

                var processes = Process.GetProcesses();
                
                // İlk olarak CPU baseline'ını oluştur
                await EstablishCpuBaseline(processes);
                
                // Kısa bekleyip tekrar ölç
                await Task.Delay(500);
                
                var processInfoTasks = processes.Select(GetProcessInfoSafeAsync);
                var processInfos = await Task.WhenAll(processInfoTasks);
                
                var validProcesses = processInfos
                    .Where(p => p != null)
                    .OrderByDescending(p => p.CpuUsage)
                    .ThenByDescending(p => p.MemoryUsage)
                    .ToList();

                CleanupProcessHandles(processes);
                CleanupCpuHistory(validProcesses);

                _logger.LogDebug("Collected {Count} process entries", validProcesses.Count);
                return validProcesses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting process information");
                throw;
            }
        }

        private async Task EstablishCpuBaseline(Process[] processes)
        {
            await Task.Run(() =>
            {
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            var processId = process.Id;
                            var currentTime = DateTime.UtcNow;
                            var currentCpuTime = process.TotalProcessorTime;
                            
                            lock (_lockObject)
                            {
                                if (!_processCpuHistory.ContainsKey(processId))
                                {
                                    _processCpuHistory[processId] = new ProcessCpuInfo
                                    {
                                        LastCpuTime = currentCpuTime,
                                        LastMeasureTime = currentTime
                                    };
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Process might have exited, ignore
                    }
                }
            });
        }

        private async Task<ProcessInfo> GetProcessInfoSafeAsync(Process process)
        {
            return await Task.Run(() =>
            {
                try
                {
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
                    _logger.LogTrace(ex, "Could not get info for process {ProcessId}", process?.Id);
                    return null;
                }
            });
        }

        private double GetCpuUsageSafe(Process process)
        {
            try
            {
                lock (_lockObject)
                {
                    var processId = process.Id;
                    var currentTime = DateTime.UtcNow;
                    var currentCpuTime = process.TotalProcessorTime;
                    
                    if (_processCpuHistory.TryGetValue(processId, out var previousInfo))
                    {
                        var cpuTimeDiff = currentCpuTime - previousInfo.LastCpuTime;
                        var realTimeDiff = currentTime - previousInfo.LastMeasureTime;
                        
                        if (realTimeDiff.TotalMilliseconds > 100) // En az 100ms geçmiş olmalı
                        {
                            // CPU percentage = (CPU time used / Real time elapsed) * 100
                            var cpuUsagePercentage = (cpuTimeDiff.TotalMilliseconds / realTimeDiff.TotalMilliseconds) * 100;
                            
                            // Clamp değerleri makul sınırlara
                            cpuUsagePercentage = Math.Max(0, Math.Min(100, cpuUsagePercentage));
                            
                            // History'yi güncelle
                            _processCpuHistory[processId] = new ProcessCpuInfo
                            {
                                LastCpuTime = currentCpuTime,
                                LastMeasureTime = currentTime
                            };
                            
                            return Math.Round(cpuUsagePercentage, 1);
                        }
                    }
                    
                    // İlk ölçüm veya çok kısa süre geçmiş - baseline kaydet
                    _processCpuHistory[processId] = new ProcessCpuInfo
                    {
                        LastCpuTime = currentCpuTime,
                        LastMeasureTime = currentTime
                    };
                    
                    return 0.0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error calculating CPU usage for process {ProcessId}", process?.Id);
                return 0.0;
            }
        }

        private string GetProcessNameSafe(Process process)
        {
            try
            {
                return process.ProcessName ?? "Unknown";
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
                var path = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(path))
                    return path;

                return GetExecutablePathFromWMI(process.Id);
            }
            catch
            {
                return "Access Denied";
            }
        }

        private string GetExecutablePathFromWMI(int processId)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}");
                
                using var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    var path = obj["ExecutablePath"]?.ToString();
                    if (!string.IsNullOrEmpty(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "WMI query failed for process {ProcessId}", processId);
            }
            
            return "Unknown";
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
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}");
                
                using var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    var ownerInfo = new string[2];
                    obj.InvokeMethod("GetOwner", ownerInfo);
                    
                    if (!string.IsNullOrEmpty(ownerInfo[0]))
                    {
                        return $"{ownerInfo[1]}\\{ownerInfo[0]}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Could not get user name for process {ProcessId}", process?.Id);
            }
            
            return "Unknown";
        }

        private bool IsSystemProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return false;
                
            return _systemProcessNames.Contains(processName);
        }

        private void CleanupProcessHandles(Process[] processes)
        {
            foreach (var process in processes)
            {
                try
                {
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Error disposing process handle {ProcessId}", process?.Id);
                }
            }
        }

        private void CleanupCpuHistory(List<ProcessInfo> currentProcesses)
        {
            try
            {
                lock (_lockObject)
                {
                    var currentProcessIds = new HashSet<int>(currentProcesses.Select(p => p.ProcessId));
                    var keysToRemove = _processCpuHistory.Keys.Where(pid => !currentProcessIds.Contains(pid)).ToList();
                    
                    foreach (var key in keysToRemove)
                    {
                        _processCpuHistory.Remove(key);
                    }
                    
                    if (keysToRemove.Count > 0)
                    {
                        _logger.LogTrace("Cleaned up CPU history for {Count} terminated processes", keysToRemove.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up CPU history");
            }
        }

        private class ProcessCpuInfo
        {
            public TimeSpan LastCpuTime { get; set; }
            public DateTime LastMeasureTime { get; set; }
        }
    }
}
