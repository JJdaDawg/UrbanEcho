using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using UrbanEcho.Models.UI;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class SignalPropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        private readonly IntersectionUI _intersection;

        public string Title => "Traffic Signal";
        public string Subtitle => "";
        public string Name => _intersection.Name;
        public string Type => _intersection.Type;
        public string Status => _intersection.Status;
        public string Municipality => _intersection.Municipality;
        public string OwnedBy => _intersection.OwnedBy;
        public string MaintainedBy => _intersection.MaintainedBy;
        public IEnumerable<string> ConnectingRoads => _intersection.ConnectingRoads;

        public SignalPropertiesViewModel(IntersectionUI intersection)
        {
            _intersection = intersection;
        }
    }
}