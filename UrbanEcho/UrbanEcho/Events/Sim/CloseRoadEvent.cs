using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    public class CloseRoadEvent : IEventForSim
    {
        private RoadEdge roadEdge;

        public CloseRoadEvent(RoadEdge roadEdge)
        {
            this.roadEdge = roadEdge;
        }

        public void Run()
        {
            UrbanEcho.Sim.Sim.CloseRoad(roadEdge);
        }
    }
}