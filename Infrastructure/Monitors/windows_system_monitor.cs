using System;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Monitors
{
    /// <summary>
    /// Windows System Monitor Implementation
    /// Responsibility: Collect system-level performance data using WMI
    /// Pattern: Adapter Pattern (WMI to our interfaces)
    /// </summary>
    public class WindowsSystemMonitor : ISystemMonitor
    {
        private readonly IPerformanceCounter _performanceCounter;
        private readonly ILogger<WindowsSystemMonitor> _logger;

        public WindowsSystemMonitor(
            IPerformanceCounter performanceCounter,
            ILogger<WindowsSystemMonitor> logger)
        {
            _performanceCounter = performanceCounter ?? throw new ArgumentNullException(nameof(performanceCounter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            try
            {
                _logger.LogDebug("Collecting system information...");

                var cpuUsageTask = _performanceCounter.GetCpuUsageAsync();       // Task<double>
                var memoryUsageTask = _performanceCounter.GetMemoryUsageAsync(); // Task<double>
                var diskUsageTask = _performanceCounter.GetDiskUsageAsync();     // Task<double>
                var memoryInfoTask = GetMemoryInfoAsync();                       // Task<(long total, long available)>
                var uptimeTask = GetSystemUptimeAsync();                         // Task<TimeSpan>

                await Task.WhenAll(cpuUsageTask, memoryUsageTask, diskUsageTask, memoryInfoTask, uptimeTask);

                var memoryInfo = await memoryInfoTask;
                var uptime = await uptimeTask;

                return new SystemInfo
                {
                    CpuUsage = await cpuUsageTask,
                    MemoryUsage = await memoryUsageTask,
                    DiskUsage = await diskUsageTask,
                    TotalMemory = memoryInfo.total,
                    AvailableMemory = memoryInfo.available,
                    ProcessCount = System.Diagnostics.Process.GetProcesses().Length,
                    SystemUptime = uptime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting system information");
                throw;
            }
        }

        private async Task<(long total, long available)> GetMemoryInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

                    using var results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        var total = Convert.ToInt64(obj["TotalVisibleMemorySize"]) * 1024;
                        var available = Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024;
                        return (total, available);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve memory info via WMI");
                }

                return (0, 0);
            });
        }

        private async Task<TimeSpan> GetSystemUptimeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    return TimeSpan.FromMilliseconds(Environment.TickCount64);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve system uptime");
                    return TimeSpan.Zero;
                }
            });
        }
    }
}
