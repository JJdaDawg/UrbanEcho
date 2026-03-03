using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models
{
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