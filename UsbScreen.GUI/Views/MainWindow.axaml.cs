using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading.Tasks;
using UsbScreen.GUI.ViewModels;
using UsbScreen.GUI.Models;
using UsbScreen.GUI.Services;

namespace UsbScreen.GUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set up drag & drop
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
        
        // Handle close-to-tray behavior
        Closing += (s, e) =>
        {
            // Skip minimize-to-tray if we're explicitly exiting
            if (App.IsExiting) return;
            
            if (SettingsService.Instance.Settings.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
            }
        };
        
        // Ensure ViewModel is hooked up
        if (DataContext is MainWindowViewModel vm)
        {
            SetupViewModel(vm);
        }
        
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                SetupViewModel(vm);
            }
        };
    }

    private void SetupViewModel(MainWindowViewModel vm)
    {
         vm.RequestImageFile = async () =>
         {
             if (!StorageProvider.CanOpen) return null;
             
             var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
             {
                 Title = "Select Image",
                 AllowMultiple = false,
                 FileTypeFilter = new[]
                 {
                     new FilePickerFileType("Images")
                     {
                         Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif" }
                     }
                 }
             });
             
             return files.Count > 0 ? files[0].Path.LocalPath : null;
         };
         
         vm.RequestFlashFile = async () =>
         {
              if (!StorageProvider.CanOpen) return null;

              var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
             {
                 Title = "Select Firmware/Image",
                 AllowMultiple = false
             });
             
             return files.Count > 0 ? files[0].Path.LocalPath : null;
         };
         
         vm.RequestSaveFile = async (defaultName, extension) =>
         {
             if (!StorageProvider.CanSave) return null;
             
             var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
             {
                 Title = "Save Preset",
                 SuggestedFileName = defaultName + extension,
                 FileTypeChoices = new[]
                 {
                     new FilePickerFileType("USB Screen Preset")
                     {
                         Patterns = new[] { "*" + extension }
                     }
                 }
             });
             
             return file?.Path.LocalPath;
         };
         
         vm.RequestOpenProjectFile = async (extension) =>
         {
             if (!StorageProvider.CanOpen) return null;
             
             var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
             {
                 Title = "Open Preset",
                 AllowMultiple = false,
                 FileTypeFilter = new[]
                 {
                     new FilePickerFileType("USB Screen Preset")
                     {
                         Patterns = new[] { "*" + extension, "*.usbscreen" }
                     }
                 }
             });
             
             return files.Count > 0 ? files[0].Path.LocalPath : null;
         };
         
         vm.RenderPageVisual = async (page) =>
         {
             // 1. Ensure visual tree is updated for the target page
             // Note: In this designer approach, we assume the UI *is* showing the target page
             // because the ViewModel sets SelectedPage before calling this, or the user is looking at it.
             
             // Wait for layout update if needed (simple delay for safety)
             // Wait for layout update if needed (simple delay for safety)
             await Dispatcher.UIThread.InvokeAsync(() => {}, DispatcherPriority.Render);

             try 
             {
                 var control = this.FindControl<Border>("DesignerBorder"); // Changed to Border to capture background
                 if (control == null) return null;

                 // 2. Render to bitmap
                 // Size is 160x80 (from XAML)
                 var bitmap = new RenderTargetBitmap(new Avalonia.PixelSize(160, 80), new Avalonia.Vector(96, 96));
                 bitmap.Render(control);

                 // 3. Convert to bytes
                 using var memoryStream = new MemoryStream();
                 bitmap.Save(memoryStream);
                 return memoryStream.ToArray();
             }
             catch(Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Render failed: {ex}");
                 return null;
             }
         };
    }
    
    // ===== Designer Interaction =====
    private bool _isDragging;
    private Point _clickPoint; // Point relative to container
    private Point _originalPosition; // Original X,Y of the layer
    private TextLayer? _capturedLayer;
    private Control? _draggedControl;

    private void OnLayerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var border = sender as Border;
        if (border?.DataContext is TextLayer layer)
        {
             var container = this.FindControl<Control>("DesignerCanvas");
             if (container == null) return;

             _capturedLayer = layer;
             _draggedControl = border;
             
             // Get click position relative to the FIXED container (The ItemsControl/Canvas)
             _clickPoint = e.GetPosition(container);
             _originalPosition = new Point(layer.X, layer.Y);
             
             _isDragging = true;
             
             // Capture pointer ensures we get events even if cursor leaves the element bounds
             e.Pointer.Capture(border);
             
             // Update selection in ViewModel
             if (DataContext is MainWindowViewModel vm)
             {
                 vm.SelectedLayer = layer;
             }
             
             e.Handled = true;
        }
    }

    private void OnLayerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _capturedLayer == null || _draggedControl == null) return;

        var container = this.FindControl<Control>("DesignerCanvas");
        if (container == null) return;

        // Ensure we are working with correct reference frame
        var currentPoint = e.GetPosition(container);
        
        // Calculate delta from the initial click point
        var delta = currentPoint - _clickPoint;

        // Apply delta to original position (Absolute Positioning)
        // This prevents drift errors from accumulating
        _capturedLayer.X = _originalPosition.X + delta.X;
        _capturedLayer.Y = _originalPosition.Y + delta.Y;
    }

    private void OnLayerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && _draggedControl != null)
        {
            _isDragging = false;
            e.Pointer.Capture(null); // Release capture
            _capturedLayer = null;
            _draggedControl = null;
            e.Handled = true;
        }
    }
    
    // ===== Drag & Drop Handlers =====
    

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.GetFiles()?.Any() == true)
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
    
    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        // Could add visual feedback removal here
    }
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files != null && DataContext is MainWindowViewModel vm)
        {
            var paths = files.Select(f => f.Path.LocalPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
            
            if (paths.Count > 0)
            {
                vm.HandleDroppedFiles(paths);
            }
        }
    }
}