using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

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
            SimManager.Instance.CloseRoad(roadEdge);
        }
    }
}