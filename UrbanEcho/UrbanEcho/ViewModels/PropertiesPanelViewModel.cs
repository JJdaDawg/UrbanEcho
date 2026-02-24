using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UrbanEcho.Events.UI;
using UrbanEcho.ViewModels.Properties;

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

            SelectedProperties = new RoadPropertiesViewModel
            {
                RoadName = "Main St",
                Aadt = "12500 vehicles/day",
                SpeedLimit = 50,
                IsRoadOpen = true,
                TruckAllowance = false
            };

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