using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;
using UrbanEcho.ViewModels.Properties;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class VehiclePropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        public string Title => "Vehicle";
        public string Subtitle => $"ID: {VehicleId}";

        [ObservableProperty] private int _vehicleId;
        [ObservableProperty] private string _status = string.Empty;
        [ObservableProperty] private string _originStreet = string.Empty;
        [ObservableProperty] private string _destinationStreet = string.Empty;
        [ObservableProperty] private bool _isEditMode;

        public RelayCommand HighlightPathCommand { get; }
        public RelayCommand ShowTrendCommand { get; }
        public RelayCommand TrackVehicleCommand { get; }

        public RelayCommand ChangeDestinationCommand { get; }
        public RelayCommand ToggleStartStopCommand { get; }
        public RelayCommand DespawnCommand { get; }

        public VehiclePropertiesViewModel()
        {
            WeakReferenceMessenger.Default.Register<EditModeChangedMessage>(this, (r, m) =>
            {
                IsEditMode = m.IsEditMode;
            });

            HighlightPathCommand = new RelayCommand(HighlightPath);
            ShowTrendCommand = new RelayCommand(ShowTrend);
            TrackVehicleCommand = new RelayCommand(TrackVehicle);
            ChangeDestinationCommand = new RelayCommand(ChangeDestination);
            ToggleStartStopCommand = new RelayCommand(ToggleStartStop);
            DespawnCommand = new RelayCommand(Despawn);
        }

        private void HighlightPath() { }
        private void ShowTrend() { }
        private void TrackVehicle() { }
        private void ChangeDestination() { }
        private void ToggleStartStop() { }
        private void Despawn() { }
    }
}