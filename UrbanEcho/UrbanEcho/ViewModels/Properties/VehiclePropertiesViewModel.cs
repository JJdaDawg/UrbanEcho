using CommunityToolkit.Mvvm.ComponentModel;
using System;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Sim;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class VehiclePropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        private readonly VehicleUI _vehicle;

        public string Title => "Vehicle";

        public string Subtitle => $"ID: {_vehicle.Id}";

        public int Id => _vehicle.Id;

        public string VehicleType => _vehicle.VehicleType;

        public float Kmh => _vehicle.Kmh;
        public float SpeedLimit => _vehicle.SpeedLimit;

        public VehicleStates State => _vehicle.State;

        public bool IsWaiting => _vehicle.IsWaiting;

        public bool WaitingOnIntersection => _vehicle.WaitingOnIntersection;

        public bool VehicleInFront => _vehicle.VehicleInFront;

        public float MetersFromCarInFront => _vehicle.MetersFromCarInFront;

        public string RoadName => _vehicle.RoadName;

        public VehiclePropertiesViewModel(VehicleUI vehicle)
        {
            _vehicle = vehicle;
        }

        public void UpdatePropertyView()
        {
            OnPropertyChanged(nameof(Kmh));
            OnPropertyChanged(nameof(SpeedLimit));
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(IsWaiting));
            OnPropertyChanged(nameof(WaitingOnIntersection));
            OnPropertyChanged(nameof(VehicleInFront));
            OnPropertyChanged(nameof(MetersFromCarInFront));
            OnPropertyChanged(nameof(RoadName));
        }
    }
}