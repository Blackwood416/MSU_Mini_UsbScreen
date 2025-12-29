using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UsbScreen.GUI.Services;

/// <summary>
/// User settings model
/// </summary>
public class AppSettings
{
    public string Theme { get; set; } = "Light";
    public string Language { get; set; } = "en-US";
    public int SlideshowIntervalSeconds { get; set; } = 5;
    public string? LastSelectedPort { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = false;
    
    [JsonIgnore]
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UsbScreen",
        "settings.json"
    );
}

/// <summary>
/// Service for persisting user settings
/// </summary>
public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private AppSettings _settings = new();
    public AppSettings Settings => _settings;

    private SettingsService()
    {
        Load();
    }

    /// <summary>
    /// Load settings from disk
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(AppSettings.SettingsPath))
            {
                var json = File.ReadAllText(AppSettings.SettingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    /// <summary>
    /// Save current settings to disk
    /// </summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(AppSettings.SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(AppSettings.SettingsPath, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }
}
