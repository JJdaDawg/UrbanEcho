using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    public class LoadFileEvent : IEventForSim
    {
        private SimEnumTypes.FileType fileType;
        private string path;
        private Map map;

        public LoadFileEvent(SimEnumTypes.FileType fileType, string path, Map map)
        {
            this.fileType = fileType;
            this.path = path;
            this.map = map;
        }

        public void Run()
        {
            if (fileType == SimEnumTypes.FileType.ProjectFile)
            {
                ProjectLayers.LoadProject(path);
            }

            PostRun();
        }

        public void PostRun()
        {
            //These Events should run on UI after loading a project
            EventQueueForUI.Instance.Add(new AddLayersEvent(map));
            EventQueueForUI.Instance.Add(new ZoomEvent(map));
        }

        public SimEnumTypes.FileType GetFileType()
        {
            return fileType;
        }
    }
}