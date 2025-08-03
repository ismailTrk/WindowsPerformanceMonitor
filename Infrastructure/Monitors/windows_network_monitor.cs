using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Monitors
{
    /// <summary>
    /// Windows Network Monitor Implementation
    /// Responsibility: Collect network statistics and connection information
    /// Pattern: Observer Pattern for network state monitoring
    /// </summary>
    public class WindowsNetworkMonitor : INetworkMonitor
    {
        private readonly ILogger<WindowsNetworkMonitor> _logger;

        public WindowsNetworkMonitor(ILogger<WindowsNetworkMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NetworkInfo> GetNetworkInfoAsync()
        {
            try
            {
                _logger.LogDebug("Collecting network information...");

                var statsTask = GetNetworkStatisticsAsync();          // Task<(long sent, long received)>
                var connectionsTask = GetActiveConnectionsAsync();    // Task<List<NetworkConnection>>

                await Task.WhenAll(statsTask, connectionsTask);

                var networkStats = await statsTask;
                var connections = await connectionsTask;

                return new NetworkInfo
                {
                    TotalBytesSent = networkStats.sent,
                    TotalBytesReceived = networkStats.received,
                    ActiveConnections = connections?.Count ?? 0,
                    Connections = connections ?? new List<NetworkConnection>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting network information");
                throw;
            }
        }

        private async Task<(long sent, long received)> GetNetworkStatisticsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                        .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                    long totalBytesSent = 0;
                    long totalBytesReceived = 0;

                    foreach (var ni in interfaces)
                    {
                        var stats = ni.GetIPv4Statistics();
                        totalBytesSent += stats.BytesSent;
                        totalBytesReceived += stats.BytesReceived;
                    }

                    return (totalBytesSent, totalBytesReceived);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve network statistics");
                    return (0, 0);
                }
            });
        }

        private async Task<List<NetworkConnection>> GetActiveConnectionsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var properties = IPGlobalProperties.GetIPGlobalProperties();
                    var tcpConnections = properties.GetActiveTcpConnections();

                    return tcpConnections.Select(conn => new NetworkConnection
                    {
                        LocalEndpoint = conn.LocalEndPoint.ToString(),
                        RemoteEndpoint = conn.RemoteEndPoint.ToString(),
                        Protocol = "TCP",
                        State = conn.State.ToString(),
                        ProcessId = 0 // Process ID mapping requires additional WMI queries
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve active connections");
                    return new List<NetworkConnection>();
                }
            });
        }
    }
}
