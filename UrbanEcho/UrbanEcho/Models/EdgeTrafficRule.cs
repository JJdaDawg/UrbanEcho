using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models
{
    /// <summary>
    /// This class provides the structure used for combining a road edge with a traffic rule
    /// </summary>
    public class EdgeTrafficRule
    {
        public RoadEdge RoadEdge;
        public TrafficRule TrafficRule;

        public EdgeTrafficRule(RoadEdge roadEdge, TrafficRule trafficRule)
        {
            RoadEdge = roadEdge;
            TrafficRule = trafficRule;
        }
    }
}