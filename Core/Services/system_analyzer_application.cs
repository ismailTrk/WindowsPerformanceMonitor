using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;
using System.Diagnostics;

namespace SystemAnalyzer.Core.Services
{
    public class SystemAnalyzerApplication : ISystemAnalyzerApplication
    {
        private readonly IConfigurationService _configService;
        private readonly IAnalysisEngine _analysisEngine;
        private readonly IConsoleDisplay _consoleDisplay;
        private readonly IUserInputHandler _inputHandler;
        private readonly IReportGenerator _reportGenerator;
        private readonly ILogger<SystemAnalyzerApplication> _logger;
        
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused = false;

        public SystemAnalyzerApplication(
            IConfigurationService configService,
            IAnalysisEngine analysisEngine,
            IConsoleDisplay consoleDisplay,
            IUserInputHandler inputHandler,
            IReportGenerator reportGenerator,
            ILogger<SystemAnalyzerApplication> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _analysisEngine = analysisEngine ?? throw new ArgumentNullException(nameof(analysisEngine));
            _consoleDisplay = consoleDisplay ?? throw new ArgumentNullException(nameof(consoleDisplay));
            _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
            _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(string[] args)
        {
            try
            {
                _logger.LogInformation("System Analyzer starting...");
                
                var config = _configService.LoadConfiguration(args);
                if (!config.IsValid)
                {
                    _consoleDisplay.ShowHelp();
                    return;
                }

                // Set output configuration
                _reportGenerator.SetOutputConfiguration(config.OutputPath, config.Format);
                _logger.LogInformation("Output configuration set: {OutputPath}, Format: {Format}", 
                    config.OutputPath, config.Format);

                _cancellationTokenSource = new CancellationTokenSource();
                
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    _cancellationTokenSource?.Cancel();
                    Console.WriteLine("\nShutting down gracefully...");
                };

                if (!config.SilentMode)
                {
                    _consoleDisplay.ShowHeader(config);
                }

                var inputTask = _inputHandler.StartMonitoringAsync(HandleUserCommand, _cancellationTokenSource.Token);
                
                await RunAnalysisLoopAsync(config, _cancellationTokenSource.Token);
                
                await _reportGenerator.GenerateFinalReportAsync();
                
                _logger.LogInformation("Analysis completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Analysis stopped by user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred: {Message}", ex.Message);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        private async Task RunAnalysisLoopAsync(AnalysisConfiguration config, CancellationToken cancellationToken)
        {
            var endTime = config.Duration > 0 
                ? DateTime.Now.AddMinutes(config.Duration) 
                : DateTime.MaxValue;

            var snapshotCount = 0;

            while (!cancellationToken.IsCancellationRequested && DateTime.Now < endTime)
            {
                try
                {
                    if (!_isPaused)
                    {
                        await PerformAnalysisStepAsync(config, ++snapshotCount);
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(config.IntervalSeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in analysis loop at snapshot {Count}", snapshotCount);
                    
                    if (!config.SilentMode)
                    {
                        Console.WriteLine($"Error in snapshot {snapshotCount}: {ex.Message}");
                    }
                }
            }

            _logger.LogInformation("Analysis loop completed. Total snapshots: {Count}", snapshotCount);
        }

        private async Task PerformAnalysisStepAsync(AnalysisConfiguration config, int snapshotNumber)
        {
            try
            {
                var snapshot = await _analysisEngine.TakeSnapshotAsync();
                
                if (!config.SilentMode)
                {
                    _consoleDisplay.DisplaySnapshot(snapshot, config);
                }
                
                await _reportGenerator.AddSnapshotAsync(snapshot);
                
                _logger.LogDebug("Snapshot {Number} completed: CPU {Cpu:F1}%, RAM {Memory:F1}%", 
                    snapshotNumber, snapshot.SystemInfo.CpuUsage, snapshot.SystemInfo.MemoryUsage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking snapshot {Number}: {Message}", snapshotNumber, ex.Message);
                throw;
            }
        }

        private void HandleUserCommand(UserCommand command)
        {
            try
            {
                switch (command.Type)
                {
                    case UserCommandType.Quit:
                        _logger.LogInformation("User requested quit");
                        _cancellationTokenSource?.Cancel();
                        break;
                        
                    case UserCommandType.Pause:
                        _isPaused = !_isPaused;
                        var message = _isPaused ? "Analysis paused" : "Analysis resumed";
                        _consoleDisplay.ShowMessage(message);
                        _logger.LogInformation("Analysis {Status}", _isPaused ? "paused" : "resumed");
                        break;
                        
                    case UserCommandType.ShowStats:
                        var stats = _analysisEngine.GetStatistics();
                        _consoleDisplay.ShowStatistics(stats);
                        break;
                        
                    case UserCommandType.Clear:
                        _consoleDisplay.Clear();
                        break;
                        
                    case UserCommandType.Help:
                        _consoleDisplay.ShowHelp();
                        break;
                        
                    default:
                        _logger.LogWarning("Unknown command: {Command}", command.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user command {Command}", command.Type);
            }
        }
    }
}