using Microsoft.Extensions.Logging;
using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;
using System.Text.Json;
using System.IO;

namespace SystemAnalyzer.Core.Services
{
    /// <summary>
    /// Configuration Service Implementation
    /// Responsibility: Parse command line arguments and manage configuration
    /// Pattern: Command Pattern for argument parsing
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AnalysisConfiguration LoadConfiguration(string[] args)
        {
            try
            {
                _logger.LogDebug("Loading configuration from command line arguments");
                
                var config = ParseCommandLineArguments(args);
                var validatedConfig = ValidateAndSanitizeConfiguration(config);
                
                _logger.LogInformation("Configuration loaded successfully: Interval={Interval}s, Duration={Duration}min, Output={Output}", 
                    validatedConfig.IntervalSeconds, validatedConfig.Duration, validatedConfig.OutputPath);
                
                return validatedConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
                return new AnalysisConfiguration { IsValid = false };
            }
        }

        public void SaveConfiguration(AnalysisConfiguration config, string filePath)
        {
            try
            {
                // Sanitize file path
                var sanitizedPath = SanitizeFilePath(filePath);
                
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(sanitizedPath, json);
                _logger.LogInformation("Configuration saved to {FilePath}", sanitizedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration to {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        public AnalysisConfiguration LoadFromFile(string filePath)
        {
            try
            {
                var sanitizedPath = SanitizeFilePath(filePath);
                
                if (!File.Exists(sanitizedPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {sanitizedPath}");
                }
                
                var json = File.ReadAllText(sanitizedPath);
                var config = JsonSerializer.Deserialize<AnalysisConfiguration>(json);
                
                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration");
                }
                
                var validatedConfig = ValidateAndSanitizeConfiguration(config);
                
                _logger.LogInformation("Configuration loaded from file: {FilePath}", sanitizedPath);
                return validatedConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration from {FilePath}", filePath);
                throw;
            }
        }

        private AnalysisConfiguration ParseCommandLineArguments(string[] args)
        {
            var config = new AnalysisConfiguration();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-i":
                    case "--interval":
                        if (TryGetNextArgument(args, i, out string intervalStr) && 
                            int.TryParse(intervalStr, out int interval))
                        {
                            config = config with { IntervalSeconds = Math.Clamp(interval, 1, 3600) }; // Max 1 hour
                            i++;
                        }
                        break;
                        
                    case "-d":
                    case "--duration":
                        if (TryGetNextArgument(args, i, out string durationStr) && 
                            int.TryParse(durationStr, out int duration))
                        {
                            config = config with { Duration = Math.Clamp(duration, 0, 1440) }; // Max 24 hours
                            i++;
                        }
                        break;
                        
                    case "-o":
                    case "--output":
                        if (TryGetNextArgument(args, i, out string outputPath))
                        {
                            var sanitizedPath = SanitizeFilePath(outputPath);
                            config = config with { OutputPath = sanitizedPath };
                            i++;
                        }
                        break;
                        
                    case "-f":
                    case "--format":
                        if (TryGetNextArgument(args, i, out string formatStr) && 
                            Enum.TryParse<ExportFormat>(formatStr, true, out var format))
                        {
                            config = config with { Format = format };
                            i++;
                        }
                        break;
                        
                    case "-v":
                    case "--verbose":
                        config = config with { Verbose = true };
                        break;
                        
                    case "-s":
                    case "--silent":
                        config = config with { SilentMode = true };
                        break;
                        
                    case "-t":
                    case "--threshold-cpu":
                        if (TryGetNextArgument(args, i, out string cpuThresholdStr) && 
                            int.TryParse(cpuThresholdStr, out int cpuThreshold))
                        {
                            config = config with { CpuThreshold = Math.Clamp(cpuThreshold, 1, 100) };
                            i++;
                        }
                        break;
                        
                    case "-m":
                    case "--threshold-mem":
                        if (TryGetNextArgument(args, i, out string memThresholdStr) && 
                            int.TryParse(memThresholdStr, out int memThreshold))
                        {
                            config = config with { MemoryThreshold = Math.Clamp(memThreshold, 1, 100) };
                            i++;
                        }
                        break;
                        
                    case "-h":
                    case "--help":
                        config = config with { IsValid = false };
                        break;
                        
                    default:
                        _logger.LogWarning("Unknown argument: {Argument}", args[i]);
                        break;
                }
            }
            
            return config;
        }

        private bool TryGetNextArgument(string[] args, int currentIndex, out string value)
        {
            value = string.Empty;
            if (currentIndex + 1 < args.Length && !args[currentIndex + 1].StartsWith("-"))
            {
                value = args[currentIndex + 1];
                return !string.IsNullOrWhiteSpace(value);
            }
            return false;
        }

        private AnalysisConfiguration ValidateAndSanitizeConfiguration(AnalysisConfiguration config)
        {
            var validatedConfig = config;
            
            // Generate output path if not specified
            if (string.IsNullOrEmpty(validatedConfig.OutputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var extension = validatedConfig.Format.ToString().ToLower();
                var defaultPath = $"system_analysis_{timestamp}.{extension}";
                validatedConfig = validatedConfig with { OutputPath = defaultPath };
                
                _logger.LogInformation("Auto-generated output path: {OutputPath}", defaultPath);
            }
            
            // Validate and sanitize output path
            validatedConfig = validatedConfig with { 
                OutputPath = SanitizeFilePath(validatedConfig.OutputPath) 
            };
            
            // Validate thresholds
            validatedConfig = validatedConfig with {
                CpuThreshold = Math.Clamp(validatedConfig.CpuThreshold, 1, 100),
                MemoryThreshold = Math.Clamp(validatedConfig.MemoryThreshold, 1, 100),
                IntervalSeconds = Math.Clamp(validatedConfig.IntervalSeconds, 1, 3600)
            };
            
            // Validate conflicting options
            if (validatedConfig.SilentMode && validatedConfig.Verbose)
            {
                _logger.LogWarning("Silent mode and verbose mode cannot be used together. Using silent mode.");
                validatedConfig = validatedConfig with { Verbose = false };
            }
            
            _logger.LogDebug("Configuration validated and sanitized successfully");
            
            return validatedConfig;
        }

        private string SanitizeFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }
            
            try
            {
                // Remove any path traversal attempts
                var fileName = Path.GetFileName(filePath);
                var directory = Path.GetDirectoryName(filePath);
                
                // If no directory specified, use current directory
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Environment.CurrentDirectory;
                }
                
                // Validate directory exists or can be created
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Sanitize filename
                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitizedFileName = string.Join("_", fileName.Split(invalidChars));
                
                // Combine and return full path
                var fullPath = Path.Combine(directory, sanitizedFileName);
                
                _logger.LogDebug("File path sanitized: {OriginalPath} -> {SanitizedPath}", filePath, fullPath);
                
                return fullPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing file path: {FilePath}", filePath);
                throw new ArgumentException($"Invalid file path: {filePath}", nameof(filePath), ex);
            }
        }
    }
}