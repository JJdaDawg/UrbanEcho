using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    public class ResetPositionEvent : IEventForSim
    {
        private UrbanEcho.Models.Vehicle? vehicle;

        public ResetPositionEvent(UrbanEcho.Models.VehicleReadOnly vehicle)
        {
            this.vehicle = SimManager.Instance.GetVehicle(vehicle);
        }

        public void Run()
        {
            if (vehicle is null) return;
            vehicle.RequestResetVehicleToNewPos();
        }
    }
}