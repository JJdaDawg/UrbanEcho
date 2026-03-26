using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.ViewModels;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.UI
{
    public class SetProjectEvent : IEventForUI
    {
        private ProjectFile? projectFile;

        public SetProjectEvent(ProjectFile? projectFile)
        {
            this.projectFile = projectFile;
        }

        public void Run()
        {
            MainViewModel? mvm = MainWindow.Instance.GetMainViewModel();
            if (mvm != null)
            {
                mvm.Project.SetProject(projectFile);
            }
        }
    }
}