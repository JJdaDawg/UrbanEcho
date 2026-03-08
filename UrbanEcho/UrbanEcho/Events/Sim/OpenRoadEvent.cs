using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    public class OpenRoadEvent : IEventForSim
    {
        private RoadEdge roadEdge;

        public OpenRoadEvent(RoadEdge roadEdge)
        {
            this.roadEdge = roadEdge;
        }

        public void Run()
        {
            UrbanEcho.Sim.Sim.OpenRoad(roadEdge);
        }
    }
}