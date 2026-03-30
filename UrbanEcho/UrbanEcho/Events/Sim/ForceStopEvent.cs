using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    public class ForceStopEvent : IEventForSim
    {
        private bool stopCommand;
        private UrbanEcho.Models.Vehicle? vehicle;

        public ForceStopEvent(UrbanEcho.Models.VehicleReadOnly vehicle, bool stopCommand)
        {
            this.stopCommand = stopCommand;
            this.vehicle = SimManager.Instance.GetVehicle(vehicle);
        }

        public void Run()
        {
            if (vehicle is null) return;
            vehicle.SetForceStop(stopCommand);
        }
    }
}