using System;
using Avalonia;
using Avalonia.Styling;

namespace UsbScreen.GUI.Services;

/// <summary>
/// Available theme options
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    System
}

/// <summary>
/// Service for managing application theme
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    public event EventHandler<AppTheme>? ThemeChanged;

    private AppTheme _currentTheme = AppTheme.Light;
    public AppTheme CurrentTheme => _currentTheme;

    private ThemeService() { }

    /// <summary>
    /// Initialize theme from saved settings
    /// </summary>
    public void Initialize()
    {
        var themeName = SettingsService.Instance.Settings.Theme;
        if (Enum.TryParse<AppTheme>(themeName, out var theme))
        {
            SetTheme(theme, saveSettings: false);
        }
    }

    /// <summary>
    /// Set application theme
    /// </summary>
    public void SetTheme(AppTheme theme, bool saveSettings = true)
    {
        _currentTheme = theme;

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                AppTheme.System => ThemeVariant.Default,
                _ => ThemeVariant.Light
            };
        }

        if (saveSettings)
        {
            SettingsService.Instance.Settings.Theme = theme.ToString();
            SettingsService.Instance.Save();
        }

        ThemeChanged?.Invoke(this, theme);
    }

    /// <summary>
    /// Toggle between Light and Dark themes
    /// </summary>
    public void ToggleTheme()
    {
        var next = _currentTheme switch
        {
            AppTheme.Light => AppTheme.Dark,
            AppTheme.Dark => AppTheme.Light,
            AppTheme.System => AppTheme.Light,
            _ => AppTheme.Light
        };
        SetTheme(next);
    }
}
