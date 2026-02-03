using Avalonia.Controls;
using Mapsui.Tiling;
using Ursa.Controls;

namespace MyApp;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();
        MyMapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
    }
}