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
    /// Refreshes data of the map and repaints it
    /// </summary>
    internal class RefreshMapEvent : IEventForUI
    {
        private Map map;

        public RefreshMapEvent(Map map)
        {
            this.map = map;
        }

        public void Run()
        {
            map.Refresh();
        }
    }
}