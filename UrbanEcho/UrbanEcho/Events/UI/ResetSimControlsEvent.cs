using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Resets the controls shown on the UI for simulation back to default values
    /// </summary>
    public class ResetSimControlEvent : IEventForUI
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