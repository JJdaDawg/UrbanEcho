using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UrbanEcho
{
    public class ProjectFile
    {
        public string PathForThisFile = "";
        public string BackgroundLayerPath = "";
        public string RoadLayerPath = "";
        public string IntersectionLayerPath = "";

        public ProjectFile()
        {
        }

        public ProjectFile(string values)
        {
        }

        public static ProjectFile? Open(string path)
        {
            ProjectFile? projectFile = null;
            /*
            try
            {
                using (StreamReader reader = new StreamReader(path)
                {
                }
                projectFile = JsonConvert.DeserializeObject()
                return new ProjectFile(json);
            }
            catch
            {
            }
            */
            return projectFile;
        }
    }
}