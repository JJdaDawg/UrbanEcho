using Mapsui.UI;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.UI
{
    internal class ZoomEvent : IEventForUI
    {
        private MapControl mapControl;

        public ZoomEvent(MapControl mapControl)
        {
            this.mapControl = mapControl;
        }

        public void Run()
        {
            ProjectLayers.ZoomToLayer(mapControl);
        }

        public string Message()
        {
            return "";
        }
    }
}