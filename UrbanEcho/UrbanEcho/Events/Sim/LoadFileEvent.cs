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
    /// <summary>
    /// Loads a file depending on the FileType <see cref="FileType"/>
    /// </summary>
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
            if (fileType == FileType.BackgroundFile)
            {
                ProjectLayers.LoadBackgroundFile(path);
            }
            if (fileType == FileType.RoadLayerFile)
            {
                ProjectLayers.LoadRoadFile(path);
            }
            if (fileType == FileType.IntersectionLayerFile)
            {
                ProjectLayers.LoadIntersectionsFile(path);
            }
            if (fileType == FileType.CensusLayerFile)
            {
                ProjectLayers.LoadCensusFile(path);
            }
        }

        public FileType GetFileType()
        {
            return fileType;
        }
    }
}