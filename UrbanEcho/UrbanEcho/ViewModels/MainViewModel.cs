using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExCSS;
using Mapsui;
using Mapsui.Nts.Editing;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Models;
using UrbanEcho.Services;

namespace UrbanEcho.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ConsoleViewModel Console { get; }
        public MapViewModel Map { get; }
        public SimulationViewModel Simulation { get; }
        public PropertiesViewModel Properties { get; }
        public ProjectExplorerViewModel ProjectExplorer { get; }
        public EditModeViewModel EditMode { get; }
        public ProjectViewModel Project { get; }
        public bool IsEditMode => EditMode.IsEditMode;

        public MainViewModel(IPanelService panelService, IFileDialogService fileDialogService)
        {
            Console = new ConsoleViewModel(panelService);
            Properties = new PropertiesViewModel(panelService);
            ProjectExplorer = new ProjectExplorerViewModel(panelService);
            Map = new MapViewModel();
            EditMode = new EditModeViewModel();
            Project = new ProjectViewModel(fileDialogService);
            Simulation = new SimulationViewModel();
        }
    }
}