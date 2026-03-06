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
                UrbanEcho.Sim.Sim.RunSimulation = false;
            }

            if (simControlType == SimControlType.Start)
            {
                UrbanEcho.Sim.Sim.RunSimulation = true;
            }

            if (simControlType == SimControlType.Pause)
            {
                UrbanEcho.Sim.Sim.RunSimulation = !UrbanEcho.Sim.Sim.RunSimulation;
            }

            if (simControlType == SimControlType.SpeedUp)
            {
                UrbanEcho.Sim.Sim.SimSpeed++;
                EventQueueForUI.Instance.Add(new LogToConsole(UrbanEcho.Sim.Sim.GetMainViewModel(), $"Simulation speed x{UrbanEcho.Sim.Sim.SimSpeed}"));
            }

            if (simControlType == SimControlType.SpeedDown)
            {
                UrbanEcho.Sim.Sim.SimSpeed--;
                EventQueueForUI.Instance.Add(new LogToConsole(UrbanEcho.Sim.Sim.GetMainViewModel(), $"Simulation speed x{UrbanEcho.Sim.Sim.SimSpeed}"));
            }
        }
    }
}