using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using UsbScreen.GUI.ViewModels;
using UsbScreen.GUI.Views;
using UsbScreen.GUI.Services;

namespace UsbScreen.GUI;

public partial class App : Application
{
    private TrayIconViewModel? _trayIconViewModel;
    
    /// <summary>
    /// Flag to force exit bypassing minimize-to-tray behavior
    /// </summary>
    public static bool IsExiting { get; set; } = false;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Set DataContext for TrayIcon
        _trayIconViewModel = new TrayIconViewModel();
        DataContext = _trayIconViewModel;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize services
        SettingsService.Instance.Load();
        ThemeService.Instance.Initialize();
        LocalizationService.Instance.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            // Configure shutdown mode - only exit when explicitly called
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Localize tray icon menu items
            LocalizeTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LocalizeTrayIcon()
    {
        // Find and update tray icon menu items with localized text
        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons != null && trayIcons.Count > 0)
        {
            var trayIcon = trayIcons[0];
            trayIcon.ToolTipText = LocalizationService.Instance["AppTitle"];
            
            if (trayIcon.Menu is NativeMenu menu)
            {
                // Update menu item headers with localized text
                foreach (var item in menu.Items)
                {
                    if (item is NativeMenuItem menuItem)
                    {
                        // Match by Command or order to set localized header
                        if (menuItem.Command == _trayIconViewModel?.ShowWindowCommand)
                        {
                            menuItem.Header = LocalizationService.Instance["TrayShowWindow"];
                        }
                        else if (menuItem.Command == _trayIconViewModel?.HideWindowCommand)
                        {
                            menuItem.Header = LocalizationService.Instance["TrayHideWindow"];
                        }
                        else if (menuItem.Command == _trayIconViewModel?.ExitCommand)
                        {
                            menuItem.Header = LocalizationService.Instance["TrayExit"];
                        }
                        else if (!menuItem.IsEnabled)
                        {
                            // Title item (disabled)
                            menuItem.Header = LocalizationService.Instance["AppTitle"];
                        }
                    }
                }
            }
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}