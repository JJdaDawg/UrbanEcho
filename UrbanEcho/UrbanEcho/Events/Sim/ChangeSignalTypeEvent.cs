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
    /// Changes a intersection's type
    /// </summary>
    public class ChangeSignalTypeEvent : IEventForSim
    {
        private RoadIntersection intersection;
        private RoadIntersection.SignalType signalType;

        public ChangeSignalTypeEvent(RoadIntersection intersection, RoadIntersection.SignalType signalType)
        {
            this.intersection = intersection;
            this.signalType = signalType;
        }

        public void Run()
        {
            intersection.ChangeSignalType(signalType);
            Task refresh = RefreshWithDelay();
        }

        public async Task RefreshWithDelay()
        {
            await Task.Delay(100);
            EventQueueForUI.Instance.Add(new IntersectionOverlayNeedsRefreshEvent(intersection.GetConnectedRoadFeatures()));
        }
    }
}