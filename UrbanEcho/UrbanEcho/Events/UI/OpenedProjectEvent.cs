using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.ViewModels;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.UI
{
    public class OpenedProjectEvent : IEventForUI
    {
        private string path;

        public OpenedProjectEvent(string path)
        {
            this.path = path;
        }

        public void Run()
        {
            MainViewModel? mvm = UrbanEcho.Sim.Sim.GetMainViewModel();
            if (mvm != null)
            {
                mvm.Project.OpenedProject(path);
            }
        }
    }
}