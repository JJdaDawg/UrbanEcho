using Avalonia.Controls.Shapes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Sim;

namespace UrbanEcho.FileManagement
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
                    EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Opened Project File {path}"));
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Failed to open Project File {ex.ToString()}"));
            }

            return projectFile;
        }

        public static void Save(ProjectFile projectFile)
        {
            if (projectFile.PathForThisFile == "")
            {
                try
                {
                    using (StreamWriter writer = File.CreateText(projectFile.PathForThisFile))
                    {
                        string s = JsonConvert.SerializeObject(projectFile);
                        writer.WriteLine(s);
                        EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Saved Project File {projectFile.PathForThisFile}"));
                    }
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Failed to load Project File {ex.ToString()}"));
                }
            }
            else
            {
                //TODO: Add event so dialog pops up and user can pick path
            }
        }

        public static void SaveAs(ProjectFile projectFile, string pathToSaveAt)
        {
            projectFile.PathForThisFile = pathToSaveAt;

            try
            {
                using (StreamWriter writer = File.CreateText(pathToSaveAt))
                {
                    string s = JsonConvert.SerializeObject(projectFile);
                    writer.WriteLine(s);
                    EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Saved Project File As {projectFile.PathForThisFile}"));
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Failed to save as project file {ex.ToString()}"));
            }
        }
    }
}