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

namespace UrbanEcho.ViewModels
{
    public enum LogSource { System, Map }

    public partial class MainViewModel : ObservableObject
    {
        public ConsoleViewModel Console { get; } = new();
        public MapViewModel Map { get; } = new();
        public SimulationViewModel Simulation { get; } = new();
    }
}