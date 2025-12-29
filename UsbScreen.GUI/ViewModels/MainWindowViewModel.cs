using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using UsbScreen.Utils;
using UsbScreen.Core.Utils;
using UsbScreen.Core.Services;
using UsbScreen.GUI.Services;
using UsbScreen.GUI.Models;

namespace UsbScreen.GUI.ViewModels;

// TextLineItem removed (replaced by TextLayer in Models)

public partial class MainWindowViewModel : ViewModelBase
{
    private SerialPort? _serialPort;
    private Timer? _keepAliveTimer;
    private System.Threading.CancellationTokenSource? _gifCts;

    public MainWindowViewModel()
    {
        RefreshPorts();
        // Defaults
        SelectedScale = "center";
        SelectedMode = "pad";
        Pages.Add(new DisplayPage { Name = LocalizationService.Instance["DefaultPageName"] });
        SelectedPage = Pages[0];
        
        // Initialize theme/language from services
        _selectedTheme = ThemeService.Instance.CurrentTheme;
        _selectedLanguage = LocalizationService.AvailableLanguages
            .FirstOrDefault(l => l.Code == SettingsService.Instance.Settings.Language)
            ?? LocalizationService.AvailableLanguages[0];

        // Periodic refresh for dynamic variables (%CPU%, %TIME%, etc.)
        var dynamicRefreshTimer = new Timer(1000);
        dynamicRefreshTimer.Elapsed += (s, e) => {
            if (IsConnected && SelectedPage != null && SelectedPage.Layers.Any(l => l.Text.Contains("%")))
            {
                Dispatcher.UIThread.Post(async () => await RefreshCurrentDisplay());
            }
        };
        dynamicRefreshTimer.Start();
    }

    // Connection
    [ObservableProperty] private string[] _availablePorts = Array.Empty<string>();
    [ObservableProperty] private string? _selectedPort;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ConnectButtonText))]
    [NotifyPropertyChangedFor(nameof(ConnectButtonColor))]
    private bool _isConnected;
    
    [ObservableProperty] private string _statusMessage = LocalizationService.Instance["StatusReady"];

    public string ConnectButtonText => IsConnected ? LocalizationService.Instance["Disconnect"] : LocalizationService.Instance["Connect"];
    public string ConnectButtonColor => IsConnected ? "#E74C3C" : "#2ECC71"; // Red / Green

    // Image
    public string[] ScaleOptions { get; } = { "center", "top", "bottom", "left", "right", "top-left", "top-right", "bottom-left", "bottom-right" };
    public string[] ModeOptions { get; } = { "pad", "box", "stretch", "crop" };
    public string[] CommonColors { get; } = { "Black", "White", "Red", "Green", "Blue", "Yellow", "Cyan", "Magenta", "Gray", "Orange", "Purple", "Brown", "Transparent" };
    
    [ObservableProperty] private string? _imagePath;
    [ObservableProperty] private string _selectedScale;
    [ObservableProperty] private string _selectedMode;
    
    // Text
    // Visual Designer
    public ObservableCollection<DisplayPage> Pages { get; } = new();
    
    [ObservableProperty] private DisplayPage _selectedPage;
    [ObservableProperty] private TextLayer? _selectedLayer;
    [ObservableProperty] private bool _isRotationEnabled;
    
    // About
    [ObservableProperty] private bool _isAboutVisible;
    public string AppVersion => "v1.0.1";
    public string AuthorName => "Blackwood";

    [RelayCommand]
    private void OpenAbout() => IsAboutVisible = true;

    [RelayCommand]
    private void CloseAbout() => IsAboutVisible = false;

    [RelayCommand]
    private void OpenGitHub() => OpenUrl("https://github.com/Blackwood/UsbScreen");

    [RelayCommand]
    private void OpenDonation() => OpenUrl("https://example.com/donate");

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if browser launch fails
        }
    }
    
    private Timer? _rotationTimer;

    // Delegate for View to render the visual tree to bitmap
    public Func<DisplayPage, Task<byte[]?>>? RenderPageVisual;

    partial void OnIsRotationEnabledChanged(bool value)
    {
        if (value) StartRotation();
        else StopRotation();
    }

    [RelayCommand]
    private void AddPage()
    {
        var newPage = new DisplayPage { Name = $"Page {Pages.Count + 1}" };
        Pages.Add(newPage);
        SelectedPage = newPage;
    }

    [RelayCommand]
    private void RemovePage(DisplayPage page)
    {
        if (Pages.Count <= 1) return; // Keep at least one page
        Pages.Remove(page);
        if (Pages.Count > 0) SelectedPage = Pages.Last();
    }

    [RelayCommand]
    private void AddLayer()
    {
        if (SelectedPage == null) return;
        var newLayer = new TextLayer { X = 10, Y = 10 };
        SelectedPage.Layers.Add(newLayer);
        SelectedLayer = newLayer;
    }

    [RelayCommand]
    private void RemoveLayer(TextLayer layer)
    {
        SelectedPage?.Layers.Remove(layer);
    }

    [RelayCommand]
    private void DuplicateLayer(TextLayer layer)
    {
        if (SelectedPage == null || layer == null) return;
        
        var duplicate = new TextLayer
        {
            X = layer.X + 5,
            Y = layer.Y + 5,
            Text = layer.Text,
            Color = layer.Color,
            FontSize = layer.FontSize,
            FontFamily = layer.FontFamily
        };
        
        SelectedPage.Layers.Add(duplicate);
        SelectedLayer = duplicate;
    }

    [RelayCommand]
    private void MoveLayerUp(TextLayer layer)
    {
        if (SelectedPage == null || layer == null) return;
        int index = SelectedPage.Layers.IndexOf(layer);
        if (index < SelectedPage.Layers.Count - 1)
        {
            SelectedPage.Layers.Move(index, index + 1);
            SelectedLayer = layer;
        }
    }

    [RelayCommand]
    private void MoveLayerDown(TextLayer layer)
    {
        if (SelectedPage == null || layer == null) return;
        int index = SelectedPage.Layers.IndexOf(layer);
        if (index > 0)
        {
            SelectedPage.Layers.Move(index, index - 1);
            SelectedLayer = layer;
        }
    }

    [RelayCommand]
    private void MoveXLeft(TextLayer layer)
    {
        if (layer == null) return;
        layer.X--;
    }

    [RelayCommand]
    private void MoveXRight(TextLayer layer)
    {
        if (layer == null) return;
        layer.X++;
    }

    [RelayCommand]
    private void MoveYUp(TextLayer layer)
    {
        if (layer == null) return;
        layer.Y--; // Up in screen coords is decreasing Y
    }

    [RelayCommand]
    private void MoveYDown(TextLayer layer)
    {
        if (layer == null) return;
        layer.Y++; // Down in screen coords is increasing Y
    }

    private async void StartRotation()
    {
        if (Pages.Count == 0) return;
        if (!EnsureConnected()) 
        {
            IsRotationEnabled = false;
            return;
        }

        // Show first page
        if (!Pages.Contains(SelectedPage)) SelectedPage = Pages[0];
        await SendPageToScreenCommand.ExecuteAsync(null);

        ScheduleNextRotation();
    }

    private void StopRotation()
    {
        _rotationTimer?.Stop();
        _rotationTimer?.Dispose();
        _rotationTimer = null;
        StatusMessage = LocalizationService.Instance["StatusRotationStopped"];
    }

    private void ScheduleNextRotation()
    {
        if (!IsRotationEnabled) return;

        _rotationTimer?.Dispose();
        _rotationTimer = new Timer(SelectedPage.DurationSeconds * 1000);
        _rotationTimer.Elapsed += async (s, e) => 
        {
             await Dispatcher.UIThread.InvokeAsync(async () => 
             {
                 if (!IsRotationEnabled) return;

                 // Move to next page
                 var index = Pages.IndexOf(SelectedPage);
                 var nextIndex = (index + 1) % Pages.Count;
                 SelectedPage = Pages[nextIndex];

                 await SendPageToScreenCommand.ExecuteAsync(null);
                 
                 ScheduleNextRotation(); // Schedule next based on NEW page duration
             });
        };
        _rotationTimer.AutoReset = false; // We manually reschedule
        _rotationTimer.Start();
    }
    
    [RelayCommand]
    public async Task RefreshCurrentDisplay()
    {
        if (SelectedPage != null)
        {
             await SendPageToScreen();
        }
        else if (!string.IsNullOrEmpty(ImagePath))
        {
             ShowImage();
        }
    }

    [RelayCommand]
    private async Task SendPageToScreen()
    {
        if (!EnsureConnected()) return;
        if (RenderPageVisual == null) 
        {
            StatusMessage = LocalizationService.Instance["StatusRendererError"];
            return;
        }

        try
        {
            // Evaluate variables for each layer
            var originalTexts = new Dictionary<TextLayer, string>();
            foreach (var layer in SelectedPage.Layers)
            {
                if (layer.Text.Contains("%"))
                {
                    originalTexts[layer] = layer.DisplayText;
                    layer.DisplayText = VariableEvaluator.Evaluate(layer.Text);
                }
            }

            var pngBytes = await RenderPageVisual(SelectedPage);

            // Restore original display text (to avoid permanent replacement in UI if desired, 
            // though keeping it evaluated in UI is also fine. We restore for next clean evaluation.)
            foreach (var kvp in originalTexts)
            {
                kvp.Key.DisplayText = kvp.Value;
            }

            if (pngBytes == null) return;

            // Send raw bytes using ImageUtil helper (we need to bypass file loading)
            using (var image = Image.Load<Rgb24>(pngBytes))
            {
                 // Resize if not 160x80 (it should be, but safety check)
                 if (image.Width != 160 || image.Height != 80)
                 {
                     image.Mutate(x => x.Resize(160, 80));
                 }
                 ImageUtil.ShowPng(_serialPort!, image);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error sending page: {ex.Message}";
        }
    }

    public string[] FlashTypes { get; } = { "Firmware", "Background", "Album", "Animation" };
    [ObservableProperty] private string? _flashFilePath;
    [ObservableProperty] private string _selectedFlashType = "Firmware";
    [ObservableProperty] private bool _showSettings;
    [ObservableProperty] private bool _restartRequired;

    // Theme & Language
    public IReadOnlyList<AppTheme> ThemeOptions { get; } = new[] { AppTheme.Light, AppTheme.Dark, AppTheme.System };
    public IReadOnlyList<LanguageInfo> LanguageOptions => LocalizationService.AvailableLanguages;
    
    [ObservableProperty] private AppTheme _selectedTheme;
    [ObservableProperty] private LanguageInfo _selectedLanguage;
    [ObservableProperty] private bool _minimizeToTrayOnClose = SettingsService.Instance.Settings.MinimizeToTrayOnClose;
    
    partial void OnMinimizeToTrayOnCloseChanged(bool value)
    {
        SettingsService.Instance.Settings.MinimizeToTrayOnClose = value;
        SettingsService.Instance.Save();
    }
    
    partial void OnSelectedThemeChanged(AppTheme value) => ThemeService.Instance.SetTheme(value);
    partial void OnSelectedLanguageChanged(LanguageInfo value)
    {
        if (value != null && value.Code != SettingsService.Instance.Settings.Language)
        {
            LocalizationService.Instance.SetLanguage(value.Code);
            StatusMessage = LocalizationService.Instance["RestartRequired"];
            RestartRequired = true;
            ShowSettings = true; 
        }
    }
    
    [RelayCommand]
    private void ToggleTheme() => ThemeService.Instance.ToggleTheme();

    [RelayCommand]
    private void OpenSettings()
    {
        ShowSettings = true;
    }
    
    [RelayCommand]
    private void CloseSettings()
    {
        ShowSettings = false;
    }

    [RelayCommand]
    private void RestartApplication()
    {
        var filename = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (filename != null)
        {
            System.Diagnostics.Process.Start(filename);
            Environment.Exit(0);
        }
    }

    // Window Controls
    [RelayCommand]
    private void MinimizeWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
        }
    }

    [RelayCommand]
    private void MaximizeWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.WindowState = desktop.MainWindow.WindowState == Avalonia.Controls.WindowState.Maximized 
                ? Avalonia.Controls.WindowState.Normal 
                : Avalonia.Controls.WindowState.Maximized;
        }
    }

    [RelayCommand]
    private void CloseWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.Close();
        }
    }

    // Continuous Display (Keep Alive)
    [ObservableProperty] private bool _isKeepAliveEnabled;
    [ObservableProperty] private int _keepAliveInterval = 7;

    partial void OnIsKeepAliveEnabledChanged(bool value)
    {
        if (value) StartKeepAlive();
        else StopKeepAlive();
    }

    private void StartKeepAlive()
    {
        StopKeepAlive();
        _keepAliveTimer = new Timer(KeepAliveInterval * 1000);
        _keepAliveTimer.Elapsed += OnKeepAliveTimerElapsed;
        _keepAliveTimer.AutoReset = true;
        _keepAliveTimer.Start();
    }

    private void StopKeepAlive()
    {
        if (_keepAliveTimer != null)
        {
            _keepAliveTimer.Stop();
            _keepAliveTimer.Elapsed -= OnKeepAliveTimerElapsed;
            _keepAliveTimer.Dispose();
            _keepAliveTimer = null;
        }
    }

    private void OnKeepAliveTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsConnected || string.IsNullOrEmpty(ImagePath) || !File.Exists(ImagePath)) return;
            try
            {
                ProcessAndShowImage(new FileInfo(ImagePath), SelectedScale, SelectedMode);
            }
            catch { }
        });
    }

    // Text Slideshow


    // Delegates for View interaction
    public Func<Task<string?>>? RequestImageFile;
    public Func<Task<string?>>? RequestFlashFile;
    public Func<string, string, Task<string?>>? RequestSaveFile; // (defaultName, filter) => path
    public Func<string, Task<string?>>? RequestOpenProjectFile; // (filter) => path

    [RelayCommand]
    private async Task BrowseImage()
    {
        if (RequestImageFile != null)
        {
            var path = await RequestImageFile();
            if (path != null) ImagePath = path;
        }
    }
    
    [RelayCommand]
    private async Task BrowseFlash()
    {
        if (RequestFlashFile != null)
        {
            var path = await RequestFlashFile();
            if (path != null) FlashFilePath = path;
        }
    }

    // ===== Save/Load Image Tab Preset =====
    
    [RelayCommand]
    private async Task SaveImagePreset()
    {
        if (RequestSaveFile == null) return;
        
        try
        {
            var path = await RequestSaveFile("image_preset", ProjectService.ImagePresetExtension);
            if (string.IsNullOrEmpty(path)) return;
            
            var data = ProjectService.CreateImageTabData(ImagePath, SelectedScale, SelectedMode, IsKeepAliveEnabled);
            await ProjectService.SaveImagePresetAsync(path, data);
            StatusMessage = LocalizationService.Instance["PresetSaved"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"{LocalizationService.Instance["ErrorPrefix"]}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadImagePreset()
    {
        if (RequestOpenProjectFile == null) return;
        
        try
        {
            var path = await RequestOpenProjectFile(ProjectService.ImagePresetExtension);
            if (string.IsNullOrEmpty(path)) return;
            
            var data = await ProjectService.LoadImagePresetAsync(path);
            if (data == null)
            {
                StatusMessage = LocalizationService.Instance["InvalidPresetFile"];
                return;
            }
            
            ImagePath = data.ImagePath;
            SelectedScale = data.Scale;
            SelectedMode = data.Mode;
            IsKeepAliveEnabled = data.KeepAliveEnabled;
            
            StatusMessage = LocalizationService.Instance["PresetLoaded"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"{LocalizationService.Instance["ErrorPrefix"]}: {ex.Message}";
        }
    }

    // ===== Save/Load Text Tab Preset =====
    
    [RelayCommand]
    private async Task SaveTextPreset()
    {
        if (RequestSaveFile == null) return;
        
        try
        {
            var path = await RequestSaveFile("text_preset", ProjectService.TextPresetExtension);
            if (string.IsNullOrEmpty(path)) return;
            
            var data = ProjectService.CreateTextTabData(Pages);
            await ProjectService.SaveTextPresetAsync(path, data);
            StatusMessage = LocalizationService.Instance["PresetSaved"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"{LocalizationService.Instance["ErrorPrefix"]}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadTextPreset()
    {
        if (RequestOpenProjectFile == null) return;
        
        try
        {
            var path = await RequestOpenProjectFile(ProjectService.TextPresetExtension);
            if (string.IsNullOrEmpty(path)) return;
            
            var data = await ProjectService.LoadTextPresetAsync(path);
            if (data == null || data.Pages.Count == 0)
            {
                StatusMessage = LocalizationService.Instance["InvalidPresetFile"];
                return;
            }
            
            // Replace current pages with loaded pages
            Pages.Clear();
            var loadedPages = ProjectService.CreateDisplayPages(data);
            foreach (var page in loadedPages)
            {
                Pages.Add(page);
            }
            
            SelectedPage = Pages[0];
            SelectedLayer = null;
            
            StatusMessage = LocalizationService.Instance["PresetLoaded"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"{LocalizationService.Instance["ErrorPrefix"]}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts = SerialPort.GetPortNames();
        
        // Try to select last used port, or fallback to first available
        var lastPort = SettingsService.Instance.Settings.LastSelectedPort;
        if (!string.IsNullOrEmpty(lastPort) && AvailablePorts.Contains(lastPort))
        {
            SelectedPort = lastPort;
        }
        else if (AvailablePorts.Any() && SelectedPort == null)
        {
            SelectedPort = AvailablePorts.First();
        }
    }

    [RelayCommand]
    private void Connect()
    {
        if (IsConnected)
        {
            Disconnect();
            return;
        }

        if (string.IsNullOrEmpty(SelectedPort))
        {
            StatusMessage = LocalizationService.Instance["NoPortSelected"];
            return;
        }

        try
        {
            _serialPort = new SerialPort
            {
                PortName = SelectedPort,
                BaudRate = 19200,
                ReadTimeout = 500
                // DTR/RTS removed to match CLI
            };
            SerialPortUtil.InitConnection(_serialPort);
            IsConnected = true;
            StatusMessage = string.Format(LocalizationService.Instance["Connected"], SelectedPort);
            
            // Remember this port for next time
            SettingsService.Instance.Settings.LastSelectedPort = SelectedPort;
            SettingsService.Instance.Save();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(LocalizationService.Instance["ConnectionFailed"], ex.Message);
            _serialPort?.Dispose();
            _serialPort = null;
            IsConnected = false;
        }
    }

    private void Disconnect()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            try { _serialPort.Close(); } catch { }
            _serialPort.Dispose();
            _serialPort = null;
        }
        IsConnected = false;
        StatusMessage = LocalizationService.Instance["Disconnected"];
    }

    [RelayCommand]
    private async Task ShowImage()
    {
        if (!EnsureConnected()) return;
        if (string.IsNullOrEmpty(ImagePath) || !File.Exists(ImagePath))
        {
            StatusMessage = LocalizationService.Instance["InvalidImageFile"];
            return;
        }

        try
        {
            StatusMessage = LocalizationService.Instance["ProcessingImage"];
            var fileInfo = new FileInfo(ImagePath);
            
            if (fileInfo.Extension.ToLower() == ".gif")
            {
                // Cancel previous GIF playback
                _gifCts?.Cancel();
                _gifCts = new System.Threading.CancellationTokenSource();
                var token = _gifCts.Token;

                // Run GIF playback in background to keep UI responsive
                _ = Task.Run(() => 
                {
                    try { ShowGifInBackground(fileInfo, SelectedScale, SelectedMode, token); }
                    catch (OperationCanceledException) { /* Normal exit */ }
                    catch (Exception ex) { Dispatcher.UIThread.Post(() => StatusMessage = $"Error: {ex.Message}"); }
                });
            }
            else
            {
                ProcessAndShowImage(fileInfo, SelectedScale, SelectedMode);
                StatusMessage = LocalizationService.Instance["ImageDisplayed"];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"{LocalizationService.Instance["ErrorPrefix"]}: {ex.Message}";
        }
    }



    [RelayCommand]
    private async Task Flash()
    {
         if (!EnsureConnected()) return;
         if (string.IsNullOrEmpty(FlashFilePath) || !File.Exists(FlashFilePath))
         {
             StatusMessage = LocalizationService.Instance["InvalidFlashFile"];
             return;
         }
         
         try
         {
             StatusMessage = LocalizationService.Instance["PreparingToFlash"];
            
             // Parse enum
             if (!Enum.TryParse<FlashImageType>(SelectedFlashType, true, out var type))
                type = FlashImageType.Firmware;

             // Create progress reporter that updates UI
             var progress = new Progress<(int current, int total, string message)>(p =>
             {
                 if (p.total > 0)
                 {
                     int percent = (int)((double)p.current / p.total * 100);
                     StatusMessage = $"{p.message} ({percent}%)";
                 }
                 else
                 {
                     StatusMessage = p.message;
                 }
             });

             // Capture serial port reference for background thread
             var port = _serialPort!;
             var filePath = FlashFilePath;

             // Run flash operation in background to avoid freezing UI
             await Task.Run(() =>
             {
                 FlashUtil.WriteImageToFlash(port, new FileInfo(filePath), type, progress);
             });
             
             StatusMessage = LocalizationService.Instance["FlashComplete"];
         }
         catch (Exception ex)
         {
             StatusMessage = $"{LocalizationService.Instance["ErrorPrefix"]}: {ex.Message}";
         }
    }



    private bool EnsureConnected()
    {
        if (!IsConnected || _serialPort == null || !_serialPort.IsOpen)
        {
            StatusMessage = LocalizationService.Instance["StatusNotConnected"];
            return false;
        }
        return true;
    }

    private void ProcessAndShowImage(FileInfo image, string scale, string mode)
    {
        using var img = SixLabors.ImageSharp.Image.Load<Rgb24>(image.FullName);
        
        var resizeOptions = new SixLabors.ImageSharp.Processing.ResizeOptions
        {
            Mode = mode switch
            {
                "pad" => SixLabors.ImageSharp.Processing.ResizeMode.Pad,
                "box" => SixLabors.ImageSharp.Processing.ResizeMode.BoxPad,
                "stretch" => SixLabors.ImageSharp.Processing.ResizeMode.Stretch,
                "crop" => SixLabors.ImageSharp.Processing.ResizeMode.Crop,
                _ => SixLabors.ImageSharp.Processing.ResizeMode.Pad
            },
            Position = GetAnchorPosition(scale),
            Sampler = SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3,
            Compand = true,
            PremultiplyAlpha = true,
            Size = new SixLabors.ImageSharp.Size(160, 80)
        };

        var clone = (image.Extension.ToLower() == ".gif") ? null : img.Clone(x => x.Resize(resizeOptions));
        
        if (image.Extension.ToLower() == ".png")
            ImageUtil.ShowPng(_serialPort!, clone!);
        else if (image.Extension.ToLower() == ".jpg" || image.Extension.ToLower() == ".jpeg")
             ImageUtil.ShowJpeg(_serialPort!, clone!);
        else if (image.Extension.ToLower() == ".gif")
        {
             ImageUtil.ShowGif(_serialPort!, img, resizeOptions, () => {
                 Dispatcher.UIThread.Post(() => StatusMessage = LocalizationService.Instance["GifPlaying"]);
             });
        }
    }
    
    private SixLabors.ImageSharp.Processing.AnchorPositionMode GetAnchorPosition(string scale)
    {
         return scale switch
            {
                "top" => SixLabors.ImageSharp.Processing.AnchorPositionMode.Top,
                "bottom" => SixLabors.ImageSharp.Processing.AnchorPositionMode.Bottom,
                "left" => SixLabors.ImageSharp.Processing.AnchorPositionMode.Left,
                "right" => SixLabors.ImageSharp.Processing.AnchorPositionMode.Right,
                "top-left" => SixLabors.ImageSharp.Processing.AnchorPositionMode.TopLeft,
                "top-right" => SixLabors.ImageSharp.Processing.AnchorPositionMode.TopRight,
                "bottom-left" => SixLabors.ImageSharp.Processing.AnchorPositionMode.BottomLeft,
                "bottom-right" => SixLabors.ImageSharp.Processing.AnchorPositionMode.BottomRight,
                _ => SixLabors.ImageSharp.Processing.AnchorPositionMode.Center
            };
    }



    // ===== Drag & Drop Support =====
    
    /// <summary>
    /// Handle dropped files - add to appropriate location based on file type
    /// </summary>
    public void HandleDroppedFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            
            if (ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp")
            {
                // Image file - set as current image
                ImagePath = file;
            }
            else if (ext is ".bin")
            {
                // Binary file - set as flash target
                FlashFilePath = file;
            }
        }
    }

    private void ShowGifInBackground(FileInfo image, string scale, string mode, System.Threading.CancellationToken token)
    {
        using var img = SixLabors.ImageSharp.Image.Load<Rgb24>(image.FullName);
        
        var resizeOptions = new SixLabors.ImageSharp.Processing.ResizeOptions
        {
            Mode = mode switch
            {
                "pad" => SixLabors.ImageSharp.Processing.ResizeMode.Pad,
                "box" => SixLabors.ImageSharp.Processing.ResizeMode.BoxPad,
                "stretch" => SixLabors.ImageSharp.Processing.ResizeMode.Stretch,
                "crop" => SixLabors.ImageSharp.Processing.ResizeMode.Crop,
                _ => SixLabors.ImageSharp.Processing.ResizeMode.Pad
            },
            Position = GetAnchorPosition(scale),
            Sampler = SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3,
            Compand = true,
            PremultiplyAlpha = true,
            Size = new SixLabors.ImageSharp.Size(160, 80)
        };

        ImageUtil.ShowGif(_serialPort!, img, resizeOptions, () => {
             Dispatcher.UIThread.Post(() => StatusMessage = LocalizationService.Instance["GifPlaying"]);
        }, token);
    }
}

