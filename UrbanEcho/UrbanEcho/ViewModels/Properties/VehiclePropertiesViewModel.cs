using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Services;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class VehiclePropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        private readonly VehicleReadOnly _vehicle;
        private readonly IVehicleService _vehicleService;

        public string Title => "Vehicle";
        public string Subtitle => $"ID: {_vehicle.Id()}";

        public int Id => _vehicle.Id();
        public string VehicleType => _vehicle.VehicleType();
        public float Kmh => _vehicle.Kmh();
        public float SpeedLimit => _vehicle.SpeedLimit();
        public VehicleStates State => _vehicle.State();
        public bool IsWaiting => _vehicle.IsWaiting();
        public bool WaitingOnIntersection => _vehicle.WaitingOnIntersection();
        public bool VehicleInFront => _vehicle.VehicleInFront();
        public float MetersFromCarInFront => _vehicle.MetersFromCarInFront();
        public string RoadName => _vehicle.RoadName();

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isTracking;

        [ObservableProperty]
        private bool _isPickingDestination;

        [ObservableProperty]
        private bool _isShowingPath;

        public VehiclePropertiesViewModel(VehicleReadOnly vehicle, IVehicleService vehicleService)
        {
            _vehicle = vehicle;
            _vehicleService = vehicleService;
            StartStopCommand = new RelayCommand(() => _vehicleService.ToggleStop(_vehicle));
            RespawnCommand = new RelayCommand(() => _vehicleService.Respawn(_vehicle));
            DestinationCommand = new RelayCommand(() =>
            {
                IsPickingDestination = !IsPickingDestination;
                if (IsPickingDestination) { _vehicleService.PickDestination(_vehicle); }
                else { _vehicleService.CancelPickDestination(); }
            });

            TrackCommand = new RelayCommand(() =>
            {
                IsTracking = !IsTracking;
                if (IsTracking) { _vehicleService.TrackVehicle(_vehicle); }
                else { _vehicleService.StopTracking(); }
            });

            PathCommand = new RelayCommand(() =>
            {
                IsShowingPath = !IsShowingPath;
                if (IsShowingPath) { _vehicleService.ShowPath(_vehicle); }
                else { _vehicleService.HidePath(); }
            });

            WeakReferenceMessenger.Default.Register<DestinationPickedMessage>(this, (r, m) => IsPickingDestination = false);
        }

        // ACTION Commands
        public RelayCommand PathCommand { get; }

        public RelayCommand TrackCommand { get; }

        // EDIT Commands
        public RelayCommand DestinationCommand { get; }

        public RelayCommand StartStopCommand { get; }
        public RelayCommand RespawnCommand { get; }

        public string StartStopLabel => _vehicle.IsForceStopped() ? "Start" : "Stop";

        public void UpdatePropertyView()
        {
            OnPropertyChanged(nameof(Kmh));
            OnPropertyChanged(nameof(SpeedLimit));
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(StartStopLabel));
            OnPropertyChanged(nameof(IsWaiting));
            OnPropertyChanged(nameof(WaitingOnIntersection));
            OnPropertyChanged(nameof(VehicleInFront));
            OnPropertyChanged(nameof(MetersFromCarInFront));
            OnPropertyChanged(nameof(RoadName));
        }
    }
}