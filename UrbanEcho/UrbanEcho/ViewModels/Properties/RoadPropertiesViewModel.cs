using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui.Nts.Editing;
using UrbanEcho.Messages;

namespace UrbanEcho.ViewModels.Properties;

public partial class RoadPropertiesViewModel : ObservableObject, IPropertiesViewModel
{
    public string Title => "Road";
    public string Subtitle => RoadName;

    [ObservableProperty] private string _roadName = string.Empty;
    [ObservableProperty] private string _aadt = string.Empty;
    [ObservableProperty] private bool _isEditMode;

    [ObservableProperty] private bool _isRoadOpen = true;
    [ObservableProperty] private int _speedLimit;
    [ObservableProperty] private bool _truckAllowance;

    [ObservableProperty] private bool _showLaneEditor;

    public RelayCommand SpawnVehicleCommand { get; }
    public RelayCommand EditLanesCommand { get; }

    public RoadPropertiesViewModel()
    {
        WeakReferenceMessenger.Default.Register<EditModeChangedMessage>(this, (r, m) =>
        {
            IsEditMode = m.IsEditMode;
            if (!IsEditMode) { ShowLaneEditor = false; }
        });

        SpawnVehicleCommand = new RelayCommand(SpawnVehicle);
        EditLanesCommand = new RelayCommand(EditLanes);
    }

    private void SpawnVehicle() { }

    private void EditLanes()
    {
        ShowLaneEditor = !ShowLaneEditor;
    }
}