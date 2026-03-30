using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Services;
using static UrbanEcho.Models.RoadIntersection;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class SignalPropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        public IEnumerable<SignalType> AvailableSignalTypes { get; } = new[]
        {
            SignalType.FullSignal,
            SignalType.TwoWayStop,
            SignalType.AllWayStop
        };

        private readonly RoadIntersection _intersection;
        private readonly IIntersectionService _intersectionService;

        public string Title => "Traffic Signal";
        public string Subtitle => "";
        public string Name => _intersection.Name;
        public SignalType Type => _intersection.TheSignalType;
        public IEnumerable<string> ConnectingRoads => _intersection.EdgesInto
            .Select(etr => etr.RoadEdge.Metadata.RoadName)
            .Concat(_intersection.EdgesOut.Select(e => e.Metadata.RoadName))
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct();

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isShowingIntersectionOverlay;

        [ObservableProperty]
        private SignalType _selectedSignalType;

        public bool IsTwoWayStop => SelectedSignalType == SignalType.TwoWayStop;

        public List<RoadSignOption> RoadSignOptions { get; private set; } = new();

        public RelayCommand ApplyStopSignAssignmentCommand { get; }

        public SignalPropertiesViewModel(RoadIntersection intersection, IIntersectionService intersectionService)
        {
            _intersection = intersection;
            _intersectionService = intersectionService;
            _selectedSignalType = intersection.TheSignalType;

            RoadSignOptions = _intersection.EdgesInto
                .Select(etr => new RoadSignOption(etr))
                .ToList();

            ApplyStopSignAssignmentCommand = new RelayCommand(() =>
            {
                _intersectionService.SetStopSignAssignment(_intersection, RoadSignOptions
                    .Select(o => (o.EdgeTrafficRule, o.HasStopSign))
                    .ToList());
                WeakReferenceMessenger.Default.Send(new ShowIntersectionOverlayMessage(_intersection));
            });
        }

        public void UpdatePropertyView()
        {
            WeakReferenceMessenger.Default.Send(new ShowIntersectionOverlayMessage(_intersection));
        }

        partial void OnSelectedSignalTypeChanged(SignalType value)
        {
            OnPropertyChanged(nameof(IsTwoWayStop));
            _intersectionService.SetSignalType(_intersection, value);

            RoadSignOptions = _intersection.EdgesInto
                .Select(etr => new RoadSignOption(etr))
                .ToList();
            OnPropertyChanged(nameof(RoadSignOptions));

            WeakReferenceMessenger.Default.Send(new ShowIntersectionOverlayMessage(_intersection));
        }
    }

    public partial class RoadSignOption : ObservableObject
    {
        public string RoadName { get; }
        public EdgeTrafficRule EdgeTrafficRule { get; }

        [ObservableProperty]
        private bool _hasStopSign;

        public RoadSignOption(EdgeTrafficRule etr)
        {
            EdgeTrafficRule = etr;
            RoadName = etr.RoadEdge.Metadata.RoadName;
            _hasStopSign = !etr.TrafficRule.IsNeverBlockingTraffic();
        }
    }
}