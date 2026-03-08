using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    public class SetSpeedLimitEvent : IEventForSim
    {
        private RoadEdge roadEdge;
        private double speed;

        public SetSpeedLimitEvent(RoadEdge roadEdge, double speed)
        {
            this.roadEdge = roadEdge;
            this.speed = speed;
        }

        public void Run()
        {
            UrbanEcho.Sim.Sim.SetSpeedLimit(roadEdge, speed);
        }
    }
}