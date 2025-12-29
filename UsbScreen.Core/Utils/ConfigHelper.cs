using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UsbScreen.Utils
{
    /// <summary>
    /// JSON source generator context for AOT-compatible serialization.
    /// </summary>
    [JsonSerializable(typeof(AppConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class AppConfigJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Cross-platform configuration helper for storing and retrieving user preferences.
    /// Configuration is stored in a JSON file under user's config directory.
    /// </summary>
    public static class ConfigHelper
    {
        private const string AppName = "UsbScreen";
        private const string ConfigFileName = "config.json";

        /// <summary>
        /// Gets the platform-specific configuration directory.
        /// Windows: %APPDATA%\UsbScreen
        /// Linux: ~/.config/UsbScreen
        /// macOS: ~/Library/Application Support/UsbScreen
        /// </summary>
        public static string GetConfigDirectory()
        {
            string configDir;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", AppName);
            }
            else // Linux and other Unix-like systems
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(xdgConfigHome))
                {
                    xdgConfigHome = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".config");
                }
                configDir = Path.Combine(xdgConfigHome, AppName);
            }

            return configDir;
        }

        /// <summary>
        /// Gets the full path to the configuration file.
        /// </summary>
        public static string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), ConfigFileName);
        }

        /// <summary>
        /// Loads the application configuration from disk.
        /// </summary>
        /// <returns>The loaded configuration, or a new default configuration if none exists.</returns>
        public static AppConfig LoadConfig()
        {
            var configPath = GetConfigFilePath();
            
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
                    return config ?? new AppConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load config: {ex.Message}");
                    return new AppConfig();
                }
            }

            return new AppConfig();
        }

        /// <summary>
        /// Saves the application configuration to disk.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        public static void SaveConfig(AppConfig config)
        {
            var configPath = GetConfigFilePath();
            var configDir = GetConfigDirectory();

            try
            {
                // Ensure the directory exists
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to save config: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the saved serial port name, or null if not set.
        /// </summary>
        public static string? GetSavedSerialPort()
        {
            var config = LoadConfig();
            return config.SerialPort;
        }

        /// <summary>
        /// Saves the serial port name to the configuration.
        /// </summary>
        /// <param name="portName">The serial port name to save.</param>
        public static void SaveSerialPort(string portName)
        {
            var config = LoadConfig();
            config.SerialPort = portName;
            SaveConfig(config);
        }
    }

    /// <summary>
    /// Application configuration model.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// The saved serial port name.
        /// </summary>
        public string? SerialPort { get; set; }
        
        /// <summary>
        /// Version of the config file format.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
