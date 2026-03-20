using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Events.UI;
using UrbanEcho.Messages;
using UrbanEcho.Models;

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

    public bool IsIntersectionLayerActive => ActiveLayer == SelectionLayer.Intersection;
    public bool IsVehicleLayerActive => ActiveLayer == SelectionLayer.Vehicle;
    public bool IsRoadLayerActive => ActiveLayer == SelectionLayer.Road;
    public bool IsSpawnerLayerActive => ActiveLayer == SelectionLayer.Spawner;

    public RelayCommand ToggleCommand { get; }
    public RelayCommand SelectIntersectionLayerCommand { get; }
    public RelayCommand SelectVehicleLayerCommand { get; }
    public RelayCommand SelectRoadLayerCommand { get; }
    public RelayCommand SelectSpawnerLayerCommand { get; }

    public ProjectExplorerPanelViewModel(IPanelService panelService)
    {
        _panelService = panelService;
        ToggleCommand = new RelayCommand(Toggle);
        SelectIntersectionLayerCommand = new RelayCommand(() => ActiveLayer = IsIntersectionLayerActive ? SelectionLayer.None : SelectionLayer.Intersection);
        SelectVehicleLayerCommand = new RelayCommand(() => ActiveLayer = IsVehicleLayerActive ? SelectionLayer.None : SelectionLayer.Vehicle);
        SelectRoadLayerCommand = new RelayCommand(() => ActiveLayer = IsRoadLayerActive ? SelectionLayer.None : SelectionLayer.Road);
        SelectSpawnerLayerCommand = new RelayCommand(() => ActiveLayer = IsSpawnerLayerActive ? SelectionLayer.None : SelectionLayer.Spawner);
        WeakReferenceMessenger.Default.Register<ProjectLoadedMessage>(this, (r, m) => HasProject = true);
        WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) => HasProject = false);
    }

    partial void OnActiveLayerChanged(SelectionLayer value)
    {
        WeakReferenceMessenger.Default.Send(new MapFeatureDeselectedMessage());
        WeakReferenceMessenger.Default.Send(new ActiveLayerChangedMessage(value));
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _panelService.ToggleProjectExplorer(_isOpen);
    }
}