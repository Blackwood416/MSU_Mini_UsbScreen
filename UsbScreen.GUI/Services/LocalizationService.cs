using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace UsbScreen.GUI.Services;

/// <summary>
/// Service for managing application localization
/// Designed for extensibility - add new languages by creating Strings.[culture].resx files
/// </summary>
public class LocalizationService
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private ResourceManager? _resourceManager;
    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public event EventHandler<CultureInfo>? LanguageChanged;

    /// <summary>
    /// Available languages. Add entries here when new .resx files are created.
    /// </summary>
    public static IReadOnlyList<LanguageInfo> AvailableLanguages { get; } = new List<LanguageInfo>
    {
        new("en-US", "English"),
        new("zh-CN", "中文")
    };

    public CultureInfo CurrentCulture => _currentCulture;

    private LocalizationService() { }

    /// <summary>
    /// Initialize localization from saved settings
    /// </summary>
    public void Initialize()
    {
        _resourceManager = new ResourceManager(
            "UsbScreen.GUI.Resources.Strings",
            typeof(LocalizationService).Assembly
        );

        var langCode = SettingsService.Instance.Settings.Language;
        if (!string.IsNullOrEmpty(langCode))
        {
            SetLanguage(langCode, saveSettings: false);
        }
    }

    /// <summary>
    /// Get localized string by key
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            return _resourceManager?.GetString(key, _currentCulture) ?? key;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// Indexer for convenient access: Localization.Instance["key"]
    /// </summary>
    public string this[string key] => GetString(key);

    /// <summary>
    /// Set current language
    /// </summary>
    public void SetLanguage(string cultureCode, bool saveSettings = true)
    {
        try
        {
            _currentCulture = new CultureInfo(cultureCode);
            CultureInfo.CurrentUICulture = _currentCulture;

            if (saveSettings)
            {
                SettingsService.Instance.Settings.Language = cultureCode;
                SettingsService.Instance.Save();
            }

            LanguageChanged?.Invoke(this, _currentCulture);
        }
        catch
        {
            // Invalid culture code, keep current
        }
    }
}

/// <summary>
/// Language info for UI display
/// </summary>
public record LanguageInfo(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
