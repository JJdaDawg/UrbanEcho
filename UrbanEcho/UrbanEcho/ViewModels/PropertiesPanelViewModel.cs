using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Events.UI;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
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

            WeakReferenceMessenger.Default.Register<MapFeatureSelectedMessage>(this, (r, m) =>
            {
                SelectedProperties = m.Type switch
                {
                    MapFeatureType.Signal when m.Data is IntersectionUI i => new SignalPropertiesViewModel(i),
                    MapFeatureType.Vehicle when m.Data is VehicleUI v => new VehiclePropertiesViewModel(v),
                    _ => null
                };
            });

            WeakReferenceMessenger.Default.Register<MapFeatureDeselectedMessage>(this, (r, m) => SelectedProperties = null);
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panelService.ToggleProperties(_isOpen);
        }
    }
}