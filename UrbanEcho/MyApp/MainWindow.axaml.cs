using Avalonia.Controls;
using Mapsui.Tiling;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MyMapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
    }
}