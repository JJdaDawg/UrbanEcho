using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    public class SetDestinationEvent : IEventForSim
    {
        private int? nearestNode;
        private UrbanEcho.Sim.Vehicle vehicle;

        public SetDestinationEvent(UrbanEcho.Sim.Vehicle vehicle, int? nearestNode)
        {
            this.nearestNode = nearestNode;
            this.vehicle = vehicle;
        }

        public void Run()
        {
            if (nearestNode is null) return;
            vehicle.SetDestination(nearestNode.Value);
        }
    }
}