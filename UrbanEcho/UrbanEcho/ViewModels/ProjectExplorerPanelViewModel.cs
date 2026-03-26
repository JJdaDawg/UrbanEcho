using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.ViewModels;

public partial class ProjectExplorerPanelViewModel : ObservableObject
{
    private readonly IPanelService _panelService;
    private bool _isOpen = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIntersectionLayerActive))]
    [NotifyPropertyChangedFor(nameof(IsVehicleLayerActive))]
    [NotifyPropertyChangedFor(nameof(IsRoadLayerActive))]
    [NotifyPropertyChangedFor(nameof(IsSpawnerLayerActive))]
    private SelectionLayer _activeLayer = SelectionLayer.None;

    [ObservableProperty] private bool _hasProject;
    [ObservableProperty] private bool _isCensusLoaded;
    [ObservableProperty] private bool _useCensusSpawning;
    [ObservableProperty] private int _routingModeIndex = 0;

    public bool IsIntersectionLayerActive => ActiveLayer == SelectionLayer.Intersection;
    public bool IsVehicleLayerActive => ActiveLayer == SelectionLayer.Vehicle;
    public bool IsRoadLayerActive => ActiveLayer == SelectionLayer.Road;
    public bool IsSpawnerLayerActive => ActiveLayer == SelectionLayer.Spawner;

    public RelayCommand ToggleCommand { get; }
    public RelayCommand SelectIntersectionLayerCommand { get; }
    public RelayCommand SelectVehicleLayerCommand { get; }
    public RelayCommand SelectRoadLayerCommand { get; }
    public RelayCommand SelectSpawnerLayerCommand { get; }
    public RelayCommand AutoPlaceGatesFromExtentCommand { get; }
    public RelayCommand AutoPlaceGatesFromResidentialCommand { get; }

    public ProjectExplorerPanelViewModel(IPanelService panelService)
    {
        _panelService = panelService;
        ToggleCommand = new RelayCommand(Toggle);
        SelectIntersectionLayerCommand = new RelayCommand(() => ActiveLayer = IsIntersectionLayerActive ? SelectionLayer.None : SelectionLayer.Intersection);
        SelectVehicleLayerCommand = new RelayCommand(() => ActiveLayer = IsVehicleLayerActive ? SelectionLayer.None : SelectionLayer.Vehicle);
        SelectRoadLayerCommand = new RelayCommand(() => ActiveLayer = IsRoadLayerActive ? SelectionLayer.None : SelectionLayer.Road);
        SelectSpawnerLayerCommand = new RelayCommand(() => ActiveLayer = IsSpawnerLayerActive ? SelectionLayer.None : SelectionLayer.Spawner);

        AutoPlaceGatesFromExtentCommand = new RelayCommand(
            () => WeakReferenceMessenger.Default.Send(new AutoPlaceSpawnersFromExtentMessage()),
            () => HasProject && IsSpawnerLayerActive);

        AutoPlaceGatesFromResidentialCommand = new RelayCommand(
            () =>
            {
                string path = ProjectLayers.GetProject()?.RoadLayerPath ?? "";
                if (!string.IsNullOrEmpty(path) && Path.GetExtension(path).Equals(".osm", System.StringComparison.OrdinalIgnoreCase))
                    WeakReferenceMessenger.Default.Send(new AutoPlaceSpawnersFromOsmResidentialMessage(path));
                else
                    WeakReferenceMessenger.Default.Send(new LogMessage("An OSM road file must be loaded to detect residential areas", LogSource.System));
            },
            () => HasProject && IsSpawnerLayerActive);

        WeakReferenceMessenger.Default.Register<ProjectLoadedMessage>(this, (r, m) =>
        {
            HasProject = true;
            AutoPlaceGatesFromExtentCommand.NotifyCanExecuteChanged();
            AutoPlaceGatesFromResidentialCommand.NotifyCanExecuteChanged();
        });
        WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) =>
        {
            HasProject = false;
            IsCensusLoaded = false;
            UseCensusSpawning = false;
            RoutingModeIndex = 0;
            AutoPlaceGatesFromExtentCommand.NotifyCanExecuteChanged();
            AutoPlaceGatesFromResidentialCommand.NotifyCanExecuteChanged();
        });

        WeakReferenceMessenger.Default.Register<CensusLoadedMessage>(this, (r, m) =>
        {
            IsCensusLoaded = true;
        });
    }

    partial void OnActiveLayerChanged(SelectionLayer value)
    {
        WeakReferenceMessenger.Default.Send(new MapFeatureDeselectedMessage());
        WeakReferenceMessenger.Default.Send(new ActiveLayerChangedMessage(value));
        AutoPlaceGatesFromExtentCommand.NotifyCanExecuteChanged();
        AutoPlaceGatesFromResidentialCommand.NotifyCanExecuteChanged();
    }

    partial void OnUseCensusSpawningChanged(bool value)
    {
        SimManager.Instance.SpawnMode = value ? SpawnMode.Census : SpawnMode.Gates;
    }

    partial void OnRoutingModeIndexChanged(int value)
    {
        SimManager.Instance.RoutingMode = value switch
        {
            1 => RoutingMode.Random,
            2 => RoutingMode.CensusOD,
            _ => RoutingMode.Aadt
        };
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _panelService.ToggleProjectExplorer(_isOpen);
    }
}