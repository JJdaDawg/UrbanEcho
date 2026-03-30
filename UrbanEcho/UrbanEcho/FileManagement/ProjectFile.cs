using Avalonia.Controls.Shapes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Events.UI;
using UrbanEcho.Sim;

namespace UrbanEcho.FileManagement
{
    public class ProjectFile
    {
        public string FileName = "";
        public string PathForThisFile = "";
        public string BackgroundLayerPath = "";
        public string RoadLayerPath = "";
        public string IntersectionLayerPath = "";
        public string CensusLayerPath = "";

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
                    //use messager to log instead of this//EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Opened Project File {path}"));
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to open Project File {ex.ToString()}"));
            }

            if (projectFile is not null)
            {
                projectFile.FileName = System.IO.Path.GetFileNameWithoutExtension(path);
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
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Saved Project File {projectFile.PathForThisFile}"));
                    }
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to load Project File {ex.ToString()}"));
                }
            }
            else
            {
                //TODO: Add event so dialog pops up and user can pick path
                //This may not be needed since save as is called instead in cases where it is blank path
            }
        }

        public static void SaveAs(ProjectFile projectFile, string pathToSaveAt)
        {
            projectFile.PathForThisFile = pathToSaveAt;
            SimManager.Instance.SetProjectNameChanged();
            try
            {
                using (StreamWriter writer = File.CreateText(pathToSaveAt))
                {
                    string s = JsonConvert.SerializeObject(projectFile);
                    writer.WriteLine(s);
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Saved Project File As {projectFile.PathForThisFile}"));
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to save as project file {ex.ToString()}"));
            }
        }
    }
}