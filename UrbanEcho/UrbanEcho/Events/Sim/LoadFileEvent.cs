using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using static UrbanEcho.FileManagement.FileTypes;

namespace UrbanEcho.Events.Sim
{
    public class LoadFileEvent : IEventForSim
    {
        private FileType fileType;
        private string path;
        private Map map;

        public LoadFileEvent(FileType fileType, string path, Map map)
        {
            this.fileType = fileType;
            this.path = path;
            this.map = map;
        }

        public void Run()
        {
            if (fileType == FileType.ProjectFile)
            {
                ProjectLayers.LoadProject(path);
            }
        }

        public FileType GetFileType()
        {
            return fileType;
        }
    }
}