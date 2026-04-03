using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Saves project using <see cref="ProjectFile.Save"/>.
    /// </summary>
    internal class SaveProjectEvent : IEventForSim
    {
        private ProjectFile projectFile;

        public SaveProjectEvent(ProjectFile projectFile)
        {
            this.projectFile = projectFile;
        }

        public void Run()
        {
            ProjectFile.Save(projectFile);
        }
    }
}