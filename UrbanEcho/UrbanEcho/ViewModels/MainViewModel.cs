using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ConsoleViewModel Console { get; } = new();
        public MapViewModel Map { get; }
        public SimulationViewModel Simulation { get; }

        public MainViewModel() 
        {
            Simulation = new(Console);
            Map = new(Console);
        }

        [ObservableProperty]
        private SelectedPanel selectedPanel = SelectedPanel.None;
    }
}