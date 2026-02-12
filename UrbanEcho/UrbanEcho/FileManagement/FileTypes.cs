using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.FileManagement
{
    public class FileTypes
    {
        public enum FileType
        {
            ProjectFile = 1,
            BackgroundFile = 2,
            RoadLayerFile = 3,
            IntersectionLayerFile = 4
        };
    }
}