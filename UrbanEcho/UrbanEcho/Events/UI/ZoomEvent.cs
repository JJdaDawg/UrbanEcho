using Mapsui;
using Mapsui.Layers;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            foreach (ILayer layer in map.Layers)
            {
                while (layer.Busy && (layer.Name == "background" || layer.Name == "Roads"))
                {
                    Thread.Sleep(100);//Wait until background and roads layer is not busy
                }
            }
            ProjectLayers.ZoomToLayer(map);

            foreach (ILayer layer in map.Layers)
            {
                while (layer.Busy && (layer.Name == "background" || layer.Name == "Roads"))
                {
                    Thread.Sleep(100);//Wait until background and roads layer is not busy
                }
            }
        }

        public string Message()
        {
            return "";
        }
    }
}