using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    public class SetTruckAllowanceEvent : IEventForSim
    {
        private RoadEdge roadEdge;
        private bool allowance;

        public SetTruckAllowanceEvent(RoadEdge roadEdge, bool allowance)
        {
            this.roadEdge = roadEdge;
            this.allowance = allowance;
        }

        public void Run()
        {
            SimManager.Instance.SetTruckAllowance(roadEdge, allowance);
        }
    }
}