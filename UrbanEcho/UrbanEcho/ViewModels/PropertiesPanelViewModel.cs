using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Events.UI;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Services;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels.Properties;
using static UrbanEcho.Models.TrafficSignal;

namespace UrbanEcho.ViewModels
{
    public partial class PropertiesPanelViewModel : ObservableObject
    {
        private readonly IPanelService _panelService;
        private readonly IVehicleService _vehicleService;
        private bool _isOpen = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        [NotifyPropertyChangedFor(nameof(Subtitle))]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        [NotifyPropertyChangedFor(nameof(ShowEmptyMapMessage))]
        private IPropertiesViewModel? _selectedProperties;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyMapMessage))]
        private bool _hasProject;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditing))]
        private bool _isEditing;

        public bool IsNotEditing => !IsEditing;

        public RelayCommand EditCommand { get; }
        public RelayCommand CancelCommand { get; }

        public bool ShowEmptyMapMessage => HasProject && !HasSelection;

        public string Title => SelectedProperties?.Title ?? "Properties";
        public string Subtitle => SelectedProperties?.Subtitle ?? string.Empty;
        public bool HasSelection => SelectedProperties is not null;

        public RelayCommand ToggleCommand { get; }

        public PropertiesPanelViewModel(IPanelService panelService, IVehicleService vehicleService)
        {
            _panelService = panelService;
            _vehicleService = vehicleService;
            ToggleCommand = new RelayCommand(Toggle);
            EditCommand = new RelayCommand(Edit);
            CancelCommand = new RelayCommand(Cancel);

            WeakReferenceMessenger.Default.Register<MapFeatureSelectedMessage>(this, (r, m) =>
            {
                IsEditing = false;
                SelectedProperties = m.Type switch
                {
                    MapFeatureType.Signal when m.Data is IntersectionUI i => new SignalPropertiesViewModel(i),
                    MapFeatureType.Vehicle when m.Data is Vehicle v => new VehiclePropertiesViewModel(v, _vehicleService),
                    MapFeatureType.Road when m.Data is RoadEdge edge => new RoadPropertiesViewModel(edge),
                    MapFeatureType.Spawner when m.Data is SpawnPoint sp => new SpawnerPropertiesViewModel(sp),
                    _ => null
                };
            });

            WeakReferenceMessenger.Default.Register<MapFeatureDeselectedMessage>(this, (r, m) =>
            {
                SelectedProperties = null;
                IsEditing = false;
            });

            WeakReferenceMessenger.Default.Register<ProjectLoadedMessage>(this, (r, m) => HasProject = true);
            WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) => HasProject = false);
        }

        private void Edit()
        {
            IsEditing = true;
            if (SelectedProperties is not null) SelectedProperties.IsEditing = true;
        }

        private void Cancel()
        {
            IsEditing = false;
            if (SelectedProperties is not null) SelectedProperties.IsEditing = false;
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panelService.ToggleProperties(_isOpen);
        }
    }
}