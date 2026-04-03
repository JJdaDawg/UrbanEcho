using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;
using static UrbanEcho.FileManagement.FileTypes;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Controls simulation managed by SimManager <see cref="SimManager"/>
    /// </summary>
    public class ControlSimEvent : IEventForSim
    {
        private SimControlType simControlType;

        public ControlSimEvent(SimControlType simControlType)
        {
            this.simControlType = simControlType;
        }

        public void Run()
        {
            if (simControlType == SimControlType.Stop)
            {
                SimManager.Instance.RunSimulation = false;
                SimManager.Instance.Paused = false;
            }

            if (simControlType == SimControlType.Start)
            {
                SimManager.Instance.RunSimulation = true;
                SimManager.Instance.Paused = false;
            }

            if (simControlType == SimControlType.Pause)
            {
                SimManager.Instance.RunSimulation = !SimManager.Instance.RunSimulation;
                SimManager.Instance.Paused = !SimManager.Instance.RunSimulation;
            }

            if (simControlType == SimControlType.SpeedUp)
            {
                SimManager.Instance.SimSpeed++;
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Simulation speed x{SimManager.Instance.SimSpeed}"));
            }

            if (simControlType == SimControlType.SpeedDown)
            {
                SimManager.Instance.SimSpeed--;
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Simulation speed x{SimManager.Instance.SimSpeed}"));
            }
        }
    }
}