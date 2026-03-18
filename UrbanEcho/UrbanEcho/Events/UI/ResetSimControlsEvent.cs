using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Events.UI
{
    internal class ResetSimControlEvent : IEventForUI
    {
        public ResetSimControlEvent()
        {
        }

        public void Run()
        {
            MainWindow.Instance.GetMainViewModel().Simulation.ResetControls();
        }
    }
}