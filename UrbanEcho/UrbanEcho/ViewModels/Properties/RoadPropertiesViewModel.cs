using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui.Nts.Editing;
using System;
using UrbanEcho.Events.Sim;
using UrbanEcho.Messages;

namespace UrbanEcho.ViewModels.Properties;

public partial class RoadPropertiesViewModel : ObservableObject, IPropertiesViewModel
{
    private readonly RoadEdge _edge;

    public string Title => "Road";
    public string Subtitle => RoadName;

    [ObservableProperty] private string _roadName = string.Empty;
    [ObservableProperty] private string _aadt = string.Empty;
    [ObservableProperty] private string _roadType = string.Empty;
    [ObservableProperty] private bool _isEditMode;

    [ObservableProperty] private bool _isRoadOpen = true;
    [ObservableProperty] private int _speedLimit;
    [ObservableProperty] private bool _truckAllowance;

    [ObservableProperty]
    private bool _isEditing;

    public RoadPropertiesViewModel(RoadEdge edge)
    {
        _edge = edge;
        RoadName = edge.Metadata.RoadName;
        Aadt = edge.Metadata.TrafficVolume > 0 ? ((int)edge.Metadata.TrafficVolume).ToString() : "N/A";
        RoadType = edge.Metadata.RoadType.ToString();
        SpeedLimit = (int)Math.Round(edge.Metadata.SpeedLimit * 3.6);
        IsRoadOpen = !edge.IsClosed;
        TruckAllowance = edge.Metadata.TruckAllowance;

        WeakReferenceMessenger.Default.Register<EditModeChangedMessage>(this, (r, m) =>
        {
            IsEditMode = m.IsEditMode;
            if (!IsEditMode) { ShowLaneEditor = false; }
        });
    }

    partial void OnIsRoadOpenChanged(bool value)
    {
        if (!IsEditing) return;

        if (value)
            EventQueueForSim.Instance.Add(new OpenRoadEvent(_edge));
        else
            EventQueueForSim.Instance.Add(new CloseRoadEvent(_edge));
    }

    partial void OnSpeedLimitChanged(int value)
    {
        if (!IsEditing) return;
        EventQueueForSim.Instance.Add(new SetSpeedLimitEvent(_edge, value / 3.6));
    }

    partial void OnTruckAllowanceChanged(bool value)
    {
        if (!IsEditing) return;
        EventQueueForSim.Instance.Add(new SetTruckAllowanceEvent(_edge, value));
    }

    private void EditLanes()
    {
        ShowLaneEditor = !ShowLaneEditor;
    }

    public void UpdatePropertyView()
    {
    }
}