using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Sets vehicle destination.
    /// </summary>
    public class SetDestinationEvent : IEventForSim
    {
        private int? nearestNode;
        private UrbanEcho.Models.Vehicle? vehicle;

        public SetDestinationEvent(UrbanEcho.Models.VehicleReadOnly vehicle, int? nearestNode)
        {
            this.nearestNode = nearestNode;
            this.vehicle = SimManager.Instance.GetVehicle(vehicle);
        }

        public void Run()
        {
            if (nearestNode is null) return;
            if (vehicle is null) return;
            vehicle.RequestSetDestination(nearestNode.Value);
        }
    }
}