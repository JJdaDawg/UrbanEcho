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

        public SignalPropertiesViewModel(RoadIntersection intersection, IIntersectionService intersectionService)
        {
            _intersection = intersection;
            _intersectionService = intersectionService;
        }

        public void UpdatePropertyView()
        {
            WeakReferenceMessenger.Default.Send(new ShowIntersectionOverlayMessage(_intersection)); 
        }
    }
}