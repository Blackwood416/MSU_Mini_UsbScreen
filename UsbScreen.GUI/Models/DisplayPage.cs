using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace UsbScreen.GUI.Models;

public partial class DisplayPage : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();

    [ObservableProperty] private string _name = "Page 1";
    [ObservableProperty] private string _backgroundColor = "Black";
    [ObservableProperty] private int _durationSeconds = 5;
    
    public ObservableCollection<TextLayer> Layers { get; } = new();
}
