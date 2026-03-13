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
            int loops = 0;
            foreach (ILayer layer in map.Layers)
            {
                while (layer.Busy)
                {
                    Thread.Sleep(100);//Wait until all layers are not busy
                    loops++;
                    if (loops > 200)
                    {
                        break;
                    }
                }
            }
            ProjectLayers.ZoomToLayer(map);
            loops = 0;

            foreach (ILayer layer in map.Layers)
            {
                while (layer.Busy)
                {
                    Thread.Sleep(100);//Wait until all layers are not busy
                    loops++;
                    if (loops > 200)
                    {
                        break;
                    }
                }
            }
        }

        public string Message()
        {
            return "";
        }
    }
}