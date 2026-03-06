using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Services;
using UrbanEcho.Sim;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class VehiclePropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        private readonly Vehicle _vehicle;
        private readonly IVehicleService _vehicleService;

        public string Title => "Vehicle";
        public string Subtitle => $"ID: {_vehicle.VehicleUI.Id}";

        public int Id => _vehicle.VehicleUI.Id;
        public string VehicleType => _vehicle.VehicleUI.VehicleType;
        public float Kmh => _vehicle.Kmh;
        public float SpeedLimit => _vehicle.SpeedLimit;
        public VehicleStates State => _vehicle.State;
        public bool IsWaiting => _vehicle.IsWaiting;
        public bool WaitingOnIntersection => _vehicle.WaitingOnIntersection;
        public bool VehicleInFront => _vehicle.VehicleInFront;
        public float MetersFromCarInFront => _vehicle.MetersFromCarInFront;
        public string RoadName => _vehicle.RoadName;

        [ObservableProperty]
        private bool _isEditing;

        public VehiclePropertiesViewModel(Vehicle vehicle, IVehicleService vehicleService)
        {
            _vehicle = vehicle;
            _vehicleService = vehicleService;
            StartStopCommand = new RelayCommand(() => _vehicleService.ToggleStop(_vehicle));
            DespawnCommand = new RelayCommand(() => _vehicleService.Despawn(_vehicle));
        }

        // ACTION Commands
        public RelayCommand PathCommand { get; } = new RelayCommand(() => { /* todo */ });
        public RelayCommand TrendCommand { get; } = new RelayCommand(() => { /* todo */ });
        public RelayCommand TrackCommand { get; } = new RelayCommand(() => { /* todo */ });
        
        // EDIT Commands
        public RelayCommand DestinationCommand { get; } = new RelayCommand(() => { /* todo */ });
        public RelayCommand StartStopCommand { get; }
        public RelayCommand DespawnCommand { get; }

        public string StartStopLabel => _vehicle.IsForceStopped ? "Start" : "Stop";

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