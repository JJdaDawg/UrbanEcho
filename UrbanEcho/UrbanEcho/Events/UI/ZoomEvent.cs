using Mapsui;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.UI
{
    internal class ZoomEvent : IEventForUI
    {
        private Map map;

        public ZoomEvent(Map map)
        {
            this.map = map;
        }

        public void Run()
        {
            ProjectLayers.ZoomToLayer(map);
        }

        public string Message()
        {
            return "";
        }
    }
}