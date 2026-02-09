using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        //https://www.newtonsoft.com/json/help/html/serializingjson.htm

        public static ProjectFile? Open(string path)
        {
            ProjectFile? projectFile = null;

            try
            {
                using (StreamReader reader = File.OpenText(path))
                {
                    string textRead = reader.ReadToEnd();
                    projectFile = JsonConvert.DeserializeObject<ProjectFile>(textRead);
                }
            }
            catch (Exception ex)
            {
                //TODO: Add error
            }

            return projectFile;
        }

        public static void Save(ProjectFile projectFile, string pathToSaveAt)
        {
            projectFile.PathForThisFile = pathToSaveAt;
            try
            {
                using (StreamWriter writer = File.CreateText(pathToSaveAt))
                {
                    string s = JsonConvert.SerializeObject(projectFile);
                    writer.WriteLine(s);
                }
            }
            catch (Exception ex)
            {
                //TODO: Add error
            }
        }
    }
}