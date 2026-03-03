using NetTopologySuite.Triangulate.QuadEdge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models
{
    public class PairedTrafficRule
    {
        public List<EdgeTrafficRule> TrafficRules = new List<EdgeTrafficRule>();

        public float CombinedAADT = 0;

        public PairedTrafficRule(List<EdgeTrafficRule> trafficRules)
        {
            TrafficRules = trafficRules;
            foreach (EdgeTrafficRule rule in trafficRules)
            {
                CombinedAADT += Helpers.Helper.TryGetFeatureKVPToFloat(rule.RoadEdge.Feature, "AADT", 0);
            }
        }
    }
}