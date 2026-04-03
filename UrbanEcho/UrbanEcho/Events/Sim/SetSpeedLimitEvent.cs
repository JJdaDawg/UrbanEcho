using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Changes the roads speed limit.
    /// </summary>
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
            SimManager.Instance.SetSpeedLimit(roadEdge, speed);
        }
    }
}