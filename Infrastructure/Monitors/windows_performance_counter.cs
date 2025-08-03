using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;

namespace SystemAnalyzer.Infrastructure.Monitors
{
    public sealed class WindowsPerformanceCounter : IPerformanceCounter, IDisposable
    {
        private readonly ILogger<WindowsPerformanceCounter> _logger;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly PerformanceCounter _diskCounter;
        private readonly object _lockObject = new object();
        
        private bool _disposed = false;
        private bool _initialized = false;
        private DateTime _lastCpuRead = DateTime.MinValue;
        private float _lastCpuValue = 0f;

        public WindowsPerformanceCounter(ILogger<WindowsPerformanceCounter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            try
            {
                _logger.LogDebug("Initializing Windows Performance Counters...");
                
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                
                // Warm up counters
                _cpuCounter.NextValue();
                _memoryCounter.NextValue();
                _diskCounter.NextValue();
                
                Thread.Sleep(100);
                
                _initialized = true;
                _logger.LogInformation("Performance Counters initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Performance Counters");
                Dispose();
                throw new InvalidOperationException("Performance Counter initialization failed", ex);
            }
        }

        public async Task<double> GetCpuUsageAsync()
        {
            ThrowIfDisposed();
            
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        var now = DateTime.Now;
                        if (now - _lastCpuRead < TimeSpan.FromMilliseconds(100))
                        {
                            return _lastCpuValue;
                        }

                        var value = _cpuCounter.NextValue();
                        
                        if (value < 0 || value > 100)
                        {
                            _logger.LogWarning("Invalid CPU reading: {Value}%. Using last known value.", value);
                            return _lastCpuValue;
                        }

                        _lastCpuValue = value;
                        _lastCpuRead = now;
                        
                        _logger.LogTrace("CPU usage: {Value:F1}%", value);
                        return value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve CPU usage, returning last known value");
                        return _lastCpuValue;
                    }
                }
            });
        }

        public async Task<double> GetMemoryUsageAsync()
        {
            ThrowIfDisposed();
            
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        var availableMB = _memoryCounter.NextValue();
                        var totalMemoryMB = 8192; // 8GB default - could be improved with WMI
                        var usedMB = Math.Max(0, totalMemoryMB - availableMB);
                        var usagePercentage = Math.Min(100, (usedMB / totalMemoryMB) * 100);
                        
                        _logger.LogTrace("Memory usage: {Used:F0}MB / {Total:F0}MB ({Percentage:F1}%)", 
                            usedMB, totalMemoryMB, usagePercentage);
                        
                        return usagePercentage;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve memory usage");
                        return 0.0;
                    }
                }
            });
        }

        public async Task<double> GetDiskUsageAsync()
        {
            ThrowIfDisposed();
            
            return await Task.Run(async () =>
            {
                try
                {
                    float value;
                    lock (_lockObject)
                    {
                        _diskCounter.NextValue();
                    }
                    
                    // Small delay outside of lock
                    await Task.Delay(50);
                    
                    lock (_lockObject)
                    {
                        value = _diskCounter.NextValue();
                    }
                    
                    value = Math.Max(0, Math.Min(100, value));
                    
                    _logger.LogTrace("Disk usage: {Value:F1}%", value);
                    return value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve disk usage");
                    return 0.0;
                }
            });
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WindowsPerformanceCounter));
            }
            
            if (!_initialized)
            {
                throw new InvalidOperationException("Performance Counter not properly initialized");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("Disposing Performance Counters...");
                
                lock (_lockObject)
                {
                    try
                    {
                        _cpuCounter?.Dispose();
                        _memoryCounter?.Dispose();
                        _diskCounter?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing performance counters");
                    }
                    
                    _disposed = true;
                }
                
                _logger.LogDebug("Performance Counters disposed");
            }
        }

        ~WindowsPerformanceCounter()
        {
            Dispose();
        }
    }
}