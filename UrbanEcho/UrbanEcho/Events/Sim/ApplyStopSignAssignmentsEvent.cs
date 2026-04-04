using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Updates assignments for a stop sign
    /// </summary>
    public class ApplyStopSignAssignmentsEvent : IEventForSim
    {
        private RoadIntersection intersection;
        private List<(EdgeTrafficRule edge, bool hasStopSign)> assignments;

        public ApplyStopSignAssignmentsEvent(RoadIntersection intersection, List<(EdgeTrafficRule edge, bool hasStopSign)> assignments)
        {
            this.intersection = intersection;
            this.assignments = assignments;
        }

        public void Run()
        {
            intersection.ApplyStopSignAssignment(assignments);
            Task refresh = RefreshWithDelay();
        }

        public async Task RefreshWithDelay()
        {
            await Task.Delay(100);
            EventQueueForUI.Instance.Add(new IntersectionOverlayNeedsRefreshEvent(intersection.GetConnectedRoadFeatures()));
        }
    }
}