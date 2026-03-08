using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui.Nts.Editing;
using System;
using UrbanEcho.Messages;

namespace UrbanEcho.ViewModels.Properties;

public partial class RoadPropertiesViewModel : ObservableObject, IPropertiesViewModel
{
    private readonly RoadEdge _edge;

    public string Title => "Road";
    public string Subtitle => RoadName;

    [ObservableProperty] private string _roadName = string.Empty;
    [ObservableProperty] private string _aadt = string.Empty;
    [ObservableProperty] private bool _isEditMode;

    [ObservableProperty] private bool _isRoadOpen = true;
    [ObservableProperty] private int _speedLimit;
    [ObservableProperty] private bool _truckAllowance;

    [ObservableProperty] private bool _showLaneEditor;

    [ObservableProperty]
    private bool _isEditing;

    public RelayCommand SpawnVehicleCommand { get; }
    public RelayCommand EditLanesCommand { get; }

    public RoadPropertiesViewModel(RoadEdge edge)
    {
        _edge = edge;
        RoadName = edge.Metadata.RoadName;
        Aadt = edge.Metadata.TrafficVolume > 0 ? ((int)edge.Metadata.TrafficVolume).ToString() : "N/A";
        SpeedLimit = (int)Math.Round(edge.Metadata.SpeedLimit * 3.6);
        IsRoadOpen = !edge.IsClosed;
        TruckAllowance = edge.Metadata.TruckAllowance;

        WeakReferenceMessenger.Default.Register<EditModeChangedMessage>(this, (r, m) =>
        {
            IsEditMode = m.IsEditMode;
            if (!IsEditMode) { ShowLaneEditor = false; }
        });

        SpawnVehicleCommand = new RelayCommand(SpawnVehicle);
        EditLanesCommand = new RelayCommand(EditLanes);
    }

    partial void OnIsRoadOpenChanged(bool value)
    {
        if (!IsEditing) return;

        if (value)
            UrbanEcho.Sim.Sim.OpenRoad(_edge);
        else
            UrbanEcho.Sim.Sim.CloseRoad(_edge);
    }

    partial void OnSpeedLimitChanged(int value)
    {
        if (!IsEditing) return;
        UrbanEcho.Sim.Sim.SetSpeedLimit(_edge, value / 3.6);
    }

    partial void OnTruckAllowanceChanged(bool value)
    {
        if (!IsEditing) return;
        UrbanEcho.Sim.Sim.SetTruckAllowance(_edge, value);
    }

    private void SpawnVehicle()
    { }

    private void EditLanes()
    {
        ShowLaneEditor = !ShowLaneEditor;
    }

    public void UpdatePropertyView()
    {
    }
}