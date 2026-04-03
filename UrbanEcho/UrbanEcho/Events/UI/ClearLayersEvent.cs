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
    /// Clears the map so that no layers are loaded.
    /// </summary>
    internal class ClearLayersEvent : IEventForUI
    {
        private Map map;

        public ClearLayersEvent(Map map)
        {
            this.map = map;
        }

        public void Run()
        {
            ProjectLayers.ClearLayers(map);
        }
    }
}