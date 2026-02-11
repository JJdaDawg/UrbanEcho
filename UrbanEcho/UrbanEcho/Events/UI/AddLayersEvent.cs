using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.UI
{
    internal class AddLayersEvent : IEventForUI
    {
        private MapControl mapControl;

        public AddLayersEvent(MapControl mapControl)
        {
            this.mapControl = mapControl;
        }

        public void Run()
        {
            ProjectLayers.AddLayers(mapControl);
        }
    }
}