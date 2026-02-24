using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UrbanEcho.Events.UI;
using UrbanEcho.ViewModels.Properties;
using static UrbanEcho.Models.TrafficSignal;

namespace UrbanEcho.ViewModels
{
    public partial class PropertiesPanelViewModel : ObservableObject
    {
        private readonly IPanelService _panelService;
        private bool _isOpen = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        [NotifyPropertyChangedFor(nameof(Subtitle))]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        private IPropertiesViewModel? _selectedProperties;

        public string Title => SelectedProperties?.Title ?? "Properties";
        public string Subtitle => SelectedProperties?.Subtitle ?? string.Empty;
        public bool HasSelection => SelectedProperties is not null;

        public RelayCommand ToggleCommand { get; }

        public PropertiesPanelViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            ToggleCommand = new RelayCommand(Toggle);

            var signal = new SignalPropertiesViewModel
            {
                SignalType = SignalType.StopSign,
                IsFourWayStop = false,
                StopSignRoad = "Elm St"
            };

            signal.ConnectingRoads.Add("Main St");
            signal.ConnectingRoads.Add("Elm St");
            signal.WaitTimes.Add(new RoadSignalStatus { RoadName = "Main St", AverageWaitTime = 15.0 });
            signal.WaitTimes.Add(new RoadSignalStatus { RoadName = "Elm St", AverageWaitTime = 6.0 });

            SelectedProperties = signal;




            //var signal = new SignalPropertiesViewModel
            //{
            //    SignalType = SignalType.Light,
            //};

            //signal.RoadStatuses.Add(new RoadSignalStatus { RoadName = "Main St", LightStatus = LightStatus.Green, AverageWaitTime = 12.5 });
            //signal.RoadStatuses.Add(new RoadSignalStatus { RoadName = "Elm St", LightStatus = LightStatus.Red, AverageWaitTime = 8.3 });

            //signal.WaitTimes.Add(new RoadSignalStatus { RoadName = "Main St", AverageWaitTime = 12.5 });
            //signal.WaitTimes.Add(new RoadSignalStatus { RoadName = "Elm St", AverageWaitTime = 8.3 });

            //signal.RoadTimings.Add(new RoadTimingConfig { RoadName = "Main St", GreenDuration = 30, ExtendedGreenDuration = 10 });
            //signal.RoadTimings.Add(new RoadTimingConfig { RoadName = "Elm St", GreenDuration = 25, ExtendedGreenDuration = 5 });

            //SelectedProperties = signal;



            //SelectedProperties = new RoadPropertiesViewModel
            //{
            //    RoadName = "Main St",
            //    Aadt = "12500 vehicles/day",
            //    SpeedLimit = 50,
            //    IsRoadOpen = true,
            //    TruckAllowance = false
            //};



            //SelectedProperties = new VehiclePropertiesViewModel
            //{
            //    VehicleId = 42,
            //    Status = "Running",
            //    OriginStreet = "Main St",
            //    DestinationStreet = "Elm St"
            //};
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panelService.ToggleProperties(_isOpen);
        }
    }
}