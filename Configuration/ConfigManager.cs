using System.Text.Json;

namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Manages engine configuration loading and saving
    /// </summary>
    public static class ConfigManager
    {
        private static EngineConfig _config;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public static EngineConfig Current
        {
            get
            {
                if (_config == null)
                {
                    lock (_lock)
                    {
                        if (_config == null)
                            LoadConfiguration();
                    }
                }
                return _config;
            }
        }

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        public static string ConfigFilePath { get; } = "engine-settings.json";

        /// <summary>
        /// Loads configuration from file or creates default
        /// </summary>
        private static void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    _config = JsonSerializer.Deserialize<EngineConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    }) ?? new EngineConfig();
                }
                else
                {
                    _config = new EngineConfig();
                    SaveConfiguration();
                }
            }
            catch (Exception)
            {
                // If loading fails, use default configuration
                _config = new EngineConfig();
            }
        }

        /// <summary>
        /// Saves current configuration to file
        /// </summary>
        public static void SaveConfiguration()
        {
            try
            {
                lock (_lock)
                {
                    var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    File.WriteAllText(ConfigFilePath, json);
                }
            }
            catch (Exception)
            {
                // Silently ignore save errors
            }
        }

        /// <summary>
        /// Reloads configuration from file
        /// </summary>
        public static void ReloadConfiguration()
        {
            lock (_lock)
            {
                _config = null;
            }
        }

        /// <summary>
        /// Updates configuration with new values
        /// </summary>
        /// <param name="updateAction">Action to update configuration</param>
        public static void UpdateConfiguration(Action<EngineConfig> updateAction)
        {
            lock (_lock)
            {
                updateAction?.Invoke(Current);
                SaveConfiguration();
            }
        }

        /// <summary>
        /// Resets configuration to defaults
        /// </summary>
        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                _config = new EngineConfig();
                SaveConfiguration();
            }
        }

        /// <summary>
        /// Validates current configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public static bool ValidateConfiguration()
        {
            try
            {
                var config = Current;

                // Basic validation
                if (string.IsNullOrWhiteSpace(config.Database.ConnectionString))
                    return false;

                if (config.Database.AutoBackupIntervalHours <= 0)
                    return false;

                if (config.Performance.CacheExpirationMinutes <= 0)
                    return false;

                if (config.Backup.RetentionDays <= 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}