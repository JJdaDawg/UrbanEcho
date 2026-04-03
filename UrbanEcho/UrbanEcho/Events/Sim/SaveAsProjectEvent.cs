using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Saves project as a new name using <see cref="ProjectFile.SaveAs"/>.
    /// </summary>
    internal class SaveAsProjectEvent : IEventForSim
    {
        private ProjectFile projectFile;
        private string path;

        public SaveAsProjectEvent(ProjectFile projectFile, string path)
        {
            this.projectFile = projectFile;
            this.path = path;
        }

        public void Run()
        {
            ProjectFile.SaveAs(projectFile, path);
        }
    }
}