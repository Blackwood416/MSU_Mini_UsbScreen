using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using UsbScreen.GUI.Services;

namespace UsbScreen.GUI;

/// <summary>
/// Markup extension for localized strings in XAML
/// Usage: Text="{local:Localize Key=AppTitle}"
/// </summary>
public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Return localized string
        return LocalizationService.Instance.GetString(Key);
    }
}
