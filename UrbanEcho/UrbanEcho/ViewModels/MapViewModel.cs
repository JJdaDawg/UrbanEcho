using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UrbanEcho.FileManagement;
using UrbanEcho.Models;

namespace UrbanEcho.ViewModels
{
    public partial class MapViewModel : ObservableObject
    {
        [ObservableProperty]
        private Map myMap = new Map();

        [ObservableProperty]
        private bool isRasterVisible = true;

        private readonly ConsoleViewModel _console;

        public MapViewModel(ConsoleViewModel console)
        {
            _console = console;
        }

        partial void OnIsRasterVisibleChanged(bool value)
        {
            ProjectLayers.IsRasterVisible = value;
            ProjectLayers.AddLayers(MyMap);
            _console.AddLog("Raster background image toggled.", LogSource.System);
        }

        [RelayCommand]
        private void ToggleRaster()
        {
            IsRasterVisible = !IsRasterVisible;
        }
    }
}
