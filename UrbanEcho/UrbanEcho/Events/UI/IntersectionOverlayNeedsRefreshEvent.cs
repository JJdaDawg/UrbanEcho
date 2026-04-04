using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Marks that intersection overlay layer needs to be redrawn
    /// </summary>
    internal class IntersectionOverlayNeedsRefreshEvent : IEventForUI
    {
        private IReadOnlyList<IFeature>? features;

        public IntersectionOverlayNeedsRefreshEvent(IReadOnlyList<IFeature>? features)
        {
            this.features = features;
        }

        public void Run()
        {
            ProjectLayers.RefreshIntersectionOverlay(features);
        }
    }
}