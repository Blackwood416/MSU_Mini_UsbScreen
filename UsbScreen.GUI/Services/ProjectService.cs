using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UsbScreen.GUI.Models;

namespace UsbScreen.GUI.Services;

/// <summary>
/// Service for saving and loading project data files
/// Cross-platform compatible using standard .NET file I/O
/// </summary>
public static class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Default file extension for project files
    /// </summary>
    public const string ProjectFileExtension = ".usbscreen";
    
    /// <summary>
    /// Default file extension for image tab presets
    /// </summary>
    public const string ImagePresetExtension = ".usbimg";
    
    /// <summary>
    /// Default file extension for text tab presets
    /// </summary>
    public const string TextPresetExtension = ".usbtxt";

    /// <summary>
    /// Save complete project data to file
    /// </summary>
    public static async Task SaveProjectAsync(string filePath, ProjectData data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Load complete project data from file
    /// </summary>
    public static async Task<ProjectData?> LoadProjectAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ProjectData>(json, JsonOptions);
    }

    /// <summary>
    /// Save only image tab data
    /// </summary>
    public static async Task SaveImagePresetAsync(string filePath, ImageTabData data)
    {
        var project = new ProjectData { ImageTab = data };
        await SaveProjectAsync(filePath, project);
    }

    /// <summary>
    /// Load only image tab data
    /// </summary>
    public static async Task<ImageTabData?> LoadImagePresetAsync(string filePath)
    {
        var project = await LoadProjectAsync(filePath);
        return project?.ImageTab;
    }

    /// <summary>
    /// Save only text tab data
    /// </summary>
    public static async Task SaveTextPresetAsync(string filePath, TextTabData data)
    {
        var project = new ProjectData { TextTab = data };
        await SaveProjectAsync(filePath, project);
    }

    /// <summary>
    /// Load only text tab data
    /// </summary>
    public static async Task<TextTabData?> LoadTextPresetAsync(string filePath)
    {
        var project = await LoadProjectAsync(filePath);
        return project?.TextTab;
    }

    /// <summary>
    /// Convert DisplayPage collection to serializable data
    /// </summary>
    public static TextTabData CreateTextTabData(IEnumerable<DisplayPage> pages)
    {
        var data = new TextTabData();
        foreach (var page in pages)
        {
            var pageData = new DisplayPageData
            {
                Name = page.Name,
                BackgroundColor = page.BackgroundColor,
                DurationSeconds = page.DurationSeconds
            };
            
            foreach (var layer in page.Layers)
            {
                pageData.Layers.Add(new TextLayerData
                {
                    X = layer.X,
                    Y = layer.Y,
                    Text = layer.Text,
                    Color = layer.Color,
                    FontSize = layer.FontSize,
                    FontFamily = layer.FontFamily
                });
            }
            
            data.Pages.Add(pageData);
        }
        return data;
    }

    /// <summary>
    /// Convert serializable data to DisplayPage objects
    /// </summary>
    public static List<DisplayPage> CreateDisplayPages(TextTabData data)
    {
        var pages = new List<DisplayPage>();
        foreach (var pageData in data.Pages)
        {
            var page = new DisplayPage
            {
                Name = pageData.Name,
                BackgroundColor = pageData.BackgroundColor,
                DurationSeconds = pageData.DurationSeconds
            };
            
            foreach (var layerData in pageData.Layers)
            {
                page.Layers.Add(new TextLayer
                {
                    X = layerData.X,
                    Y = layerData.Y,
                    Text = layerData.Text,
                    Color = layerData.Color,
                    FontSize = layerData.FontSize,
                    FontFamily = layerData.FontFamily
                });
            }
            
            pages.Add(page);
        }
        return pages;
    }

    /// <summary>
    /// Create ImageTabData from current settings
    /// </summary>
    public static ImageTabData CreateImageTabData(string? imagePath, string scale, string mode, bool keepAlive)
    {
        return new ImageTabData
        {
            ImagePath = imagePath,
            Scale = scale,
            Mode = mode,
            KeepAliveEnabled = keepAlive
        };
    }
}
