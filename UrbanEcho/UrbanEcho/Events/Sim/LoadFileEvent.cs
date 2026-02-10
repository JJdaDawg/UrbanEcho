using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    public class LoadFileEvent : IEventForSim
    {
        private SimEnumTypes.FileType fileType;
        private string path;

        public LoadFileEvent(SimEnumTypes.FileType fileType, string path)
        {
            this.fileType = fileType;
            this.path = path;
        }

        public void Run()
        {
            if (fileType == SimEnumTypes.FileType.ProjectFile)
            {
                ProjectLayers.LoadProject(path);
            }
        }

        public SimEnumTypes.FileType GetFileType()
        {
            return fileType;
        }

        public string Message()
        {
            return "";
        }
    }
}