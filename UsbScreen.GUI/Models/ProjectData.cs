using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UsbScreen.GUI.Models;

/// <summary>
/// Represents saved image tab settings
/// </summary>
public class ImageTabData
{
    public string? ImagePath { get; set; }
    public string Scale { get; set; } = "center";
    public string Mode { get; set; } = "pad";
    public bool KeepAliveEnabled { get; set; }
}

/// <summary>
/// Represents a serializable text layer
/// </summary>
public class TextLayerData
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Text { get; set; } = "Text";
    public string Color { get; set; } = "White";
    public int FontSize { get; set; } = 16;
    public string FontFamily { get; set; } = "Arial";
}

/// <summary>
/// Represents a serializable display page
/// </summary>
public class DisplayPageData
{
    public string Name { get; set; } = "Page 1";
    public string BackgroundColor { get; set; } = "Black";
    public int DurationSeconds { get; set; } = 5;
    public List<TextLayerData> Layers { get; set; } = new();
}

/// <summary>
/// Represents the complete text tab project data
/// </summary>
public class TextTabData
{
    public List<DisplayPageData> Pages { get; set; } = new();
}

/// <summary>
/// Unified project file containing both image and text configurations
/// </summary>
public class ProjectData
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
    
    [JsonPropertyName("imageTab")]
    public ImageTabData? ImageTab { get; set; }
    
    [JsonPropertyName("textTab")]
    public TextTabData? TextTab { get; set; }
}
