using Mapsui;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Adds all the layers to the map.
    /// </summary>
    internal class AddLayersEvent : IEventForUI
    {
        private Map map;

        public AddLayersEvent(Map map)
        {
            this.map = map;
        }

        public void Run()
        {
            ProjectLayers.AddLayers(map);
        }
    }
}