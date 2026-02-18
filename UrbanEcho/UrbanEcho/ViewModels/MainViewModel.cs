using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExCSS;
using Mapsui;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Models;

namespace UrbanEcho.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ConsoleViewModel Console { get; }
        public MapViewModel Map { get; }
        public SimulationViewModel Simulation { get; }
        public PropertiesViewModel Properties { get; }
        public ProjectExplorerViewModel ProjectExplorer { get; }

        public MainViewModel(IPanelService panelService)
        {
            Console = new ConsoleViewModel(panelService);
            Properties = new PropertiesViewModel(panelService);
            ProjectExplorer = new ProjectExplorerViewModel(panelService);
            Simulation = new(Console);
            Map = new(Console);
        }
    }
}