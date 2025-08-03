using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.UI
{
    /// <summary>
    /// User Input Handler Implementation
    /// Responsibility: Monitor and process user keyboard input
    /// Pattern: Command Pattern, Observer Pattern
    /// </summary>
    public class UserInputHandler : IUserInputHandler
    {
        private readonly ILogger<UserInputHandler> _logger;

        public UserInputHandler(ILogger<UserInputHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartMonitoringAsync(Action<UserCommand> commandHandler, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                _logger.LogDebug("Starting user input monitoring...");
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (Console.KeyAvailable)
                        {
                            var keyInfo = Console.ReadKey(true);
                            var command = MapKeyToCommand(keyInfo.Key);
                            
                            if (command != null)
                            {
                                _logger.LogDebug("User command received: {Command}", command.Type);
                                commandHandler(command);
                            }
                        }
                        
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in user input monitoring");
                    }
                }
                
                _logger.LogDebug("User input monitoring stopped");
            }, cancellationToken);
        }

        private UserCommand MapKeyToCommand(ConsoleKey key)
        {
            return key switch
            {
                ConsoleKey.Q => new UserCommand { Type = UserCommandType.Quit },
                ConsoleKey.P => new UserCommand { Type = UserCommandType.Pause },
                ConsoleKey.S => new UserCommand { Type = UserCommandType.ShowStats },
                ConsoleKey.C => new UserCommand { Type = UserCommandType.Clear },
                ConsoleKey.H => new UserCommand { Type = UserCommandType.Help },
                ConsoleKey.F1 => new UserCommand { Type = UserCommandType.Help },
                _ => null
            };
        }
    }
}