using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.Sim
{
    public class NewProjectEvent : IEventForSim
    {
        private Map map;

        public NewProjectEvent(Map map)
        {
            this.map = map;
        }

        public void Run()
        {
            ProjectLayers.NewProject();

            PostRun();
        }

        public void PostRun()
        {
            //These Events should run on UI after loading a project
            EventQueueForUI.Instance.Add(new ClearLayersEvent(map));
            EventQueueForUI.Instance.Add(new ZoomEvent(map));
        }
    }
}