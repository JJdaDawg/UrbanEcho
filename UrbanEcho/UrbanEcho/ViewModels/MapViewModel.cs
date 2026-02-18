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

        [ObservableProperty]
        private bool isIntersectionsVisible = true;

        private readonly ConsoleViewModel _console;

        public MapViewModel(ConsoleViewModel console)
        {
            _console = console;
        }

        partial void OnIsRasterVisibleChanged(bool value)
        {
            ProjectLayers.IsRasterVisible = value;
            ProjectLayers.AddLayers(MyMap);
            _console.AddLog("Raster background image toggled", LogSource.Map);
        }

        partial void OnIsIntersectionsVisibleChanged(bool value)
        {
            ProjectLayers.IsIntersectionsVisible = value;
            ProjectLayers.AddLayers(MyMap);
            _console.AddLog("Intersection details toggled", LogSource.Map);
        }

        [RelayCommand]
        private void ToggleRaster()
        {
            IsRasterVisible = !IsRasterVisible;
        }

        [RelayCommand]
        private void ToggleIntersectionDetails()
        {
            IsIntersectionsVisible = !IsIntersectionsVisible;
        }
    }
}
