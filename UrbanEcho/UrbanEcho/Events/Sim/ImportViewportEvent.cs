using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.Sim
{
    public class ImportViewportEvent : IEventForSim
    {
        public ImportViewportEvent()
        {
        }

        public void Run()
        {
            OsmData osmData = new OsmData();
            osmData.StartImport();
        }
    }
}