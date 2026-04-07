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
    /// <summary>
    /// Project file is a json file that lists all the paths for loading data when a project
    /// is opened.
    /// </summary>
    public class ProjectFile
    {
        public string FileName;
        public string PathForThisFile;
        public string BackgroundLayerPath;
        public string RoadLayerPath;
        public string IntersectionLayerPath;
        public string CensusLayerPath;

        public ProjectFile()
        {
            FileName = "";
            PathForThisFile = "";
            BackgroundLayerPath = "";
            RoadLayerPath = "";
            IntersectionLayerPath = "";
            CensusLayerPath = "";
        }

        //This class is serialized using the process described here
        //https://www.newtonsoft.com/json/help/html/serializingjson.htm
        /// <summary>
        /// Opens a project file. The project file is opened and deserialized using json text in the file
        /// </summary>
        /// <returns>Returns the <see cref="ProjectFile"/> </returns>
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
                if (!Helpers.Helper.TestMode)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to open Project File {ex.ToString()}"));
                }
            }

            if (projectFile is not null)
            {
                projectFile.FileName = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            return projectFile;
        }

        /// <summary>
        /// Saves the project file. The project file is serialized using json text and saved to file
        /// </summary>
        /// <returns>Returns the <see cref="ProjectFile"/> </returns>
        public static void Save(ProjectFile projectFile)
        {
            if (projectFile.PathForThisFile != "")
            {
                try
                {
                    using (StreamWriter writer = File.CreateText(projectFile.PathForThisFile))
                    {
                        string s = JsonConvert.SerializeObject(projectFile);
                        writer.WriteLine(s);
                        if (!Helpers.Helper.TestMode)
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Saved Project File {projectFile.PathForThisFile}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!Helpers.Helper.TestMode)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to save Project File {ex.ToString()}"));
                    }
                }
            }
        }

        /// <summary>
        /// Saves the project file. The project file is serialized using json text and saved to file with a path name
        /// </summary>
        /// <returns>Returns the <see cref="ProjectFile"/> </returns>
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
                    if (!Helpers.Helper.TestMode)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Saved Project File As {projectFile.PathForThisFile}"));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!Helpers.Helper.TestMode)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to save as project file {ex.ToString()}"));
                }
            }
        }
    }
}