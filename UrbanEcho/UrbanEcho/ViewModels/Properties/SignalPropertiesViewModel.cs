using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using UrbanEcho.Messages;
using static UrbanEcho.Models.TrafficSignal;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class RoadSignalStatus : ObservableObject
    {
        [ObservableProperty] private string _roadName = string.Empty;
        [ObservableProperty] private LightStatus _lightStatus;
        [ObservableProperty] private double _averageWaitTime;
    }

    public partial class RoadTimingConfig : ObservableObject
    {
        [ObservableProperty] private string _roadName = string.Empty;
        [ObservableProperty] private int _greenDuration;
        [ObservableProperty] private int _extendedGreenDuration;
        [ObservableProperty] private bool _hasTurnLaneRestriction;
    }

    public partial class SignalPropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        public string Title => "Traffic Signal";
        public string Subtitle => SignalType.ToString();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTrafficLight))]
        [NotifyPropertyChangedFor(nameof(IsStopOrYield))]
        [NotifyPropertyChangedFor(nameof(IsTrafficLightEditMode))]
        [NotifyPropertyChangedFor(nameof(IsStopOrYieldEditMode))]
        private SignalType _signalType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTrafficLightEditMode))]
        [NotifyPropertyChangedFor(nameof(IsStopOrYieldEditMode))]
        private bool _isEditMode;

        [ObservableProperty] private bool _isFourWayStop;
        [ObservableProperty] private string _stopSignRoad = string.Empty;

        public bool IsTrafficLight => SignalType == SignalType.Light;
        public bool IsStopOrYield => SignalType == SignalType.StopSign || SignalType == SignalType.YieldSign;
        public bool IsTrafficLightEditMode => IsEditMode && IsTrafficLight;
        public bool IsStopOrYieldEditMode => IsEditMode && IsStopOrYield;

        public ObservableCollection<string> ConnectingRoads { get; } = new();
        public ObservableCollection<RoadSignalStatus> RoadStatuses { get; } = new();
        public ObservableCollection<RoadSignalStatus> WaitTimes { get; } = new();
        public ObservableCollection<RoadTimingConfig> RoadTimings { get; } = new();

        public SignalPropertiesViewModel()
        {
            WeakReferenceMessenger.Default.Register<EditModeChangedMessage>(this, (r, m) =>
            {
                IsEditMode = m.IsEditMode;
            });
        }
    }
}