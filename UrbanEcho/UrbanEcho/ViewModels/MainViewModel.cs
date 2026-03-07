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
        public ConsolePanelViewModel Console { get; }
        public MapViewModel Map { get; }
        public SimulationViewModel Simulation { get; }
        public PropertiesPanelViewModel Properties { get; }
        public ProjectExplorerPanelViewModel ProjectExplorer { get; }
        public EditModeViewModel EditMode { get; }
        public ProjectViewModel Project { get; }
        public bool IsEditMode => EditMode.IsEditMode;

        public MainViewModel(IPanelService panelService, IFileDialogService fileDialogService, IMapFeatureService mapFeatureService, IVehicleService vehicleService)
        {
            Console = new ConsolePanelViewModel(panelService);
            Properties = new PropertiesPanelViewModel(panelService, vehicleService);
            ProjectExplorer = new ProjectExplorerPanelViewModel(panelService);
            Map = new MapViewModel(mapFeatureService);
            EditMode = new EditModeViewModel();
            Project = new ProjectViewModel(fileDialogService);
            Simulation = new SimulationViewModel();
        }
    }
}