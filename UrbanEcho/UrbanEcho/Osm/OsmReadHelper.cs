using Mapsui.Nts;
using OsmSharp;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;
using Node = OsmSharp.Node;

namespace UrbanEcho.Osm
{
    public static class OsmReadHelper
    {
        private static List<string> AllowableRoadTypes = PopulateAllowableRoadTypes();
        private static List<string> AllowableIntersectionTypes = PopulateAllowableIntersectionTypes();

        public static bool CheckRoadTypeAllowed(string stringToCheck)
        {
            bool returnValue = false;
            if (AllowableRoadTypes.Count == 0)
            {
                PopulateAllowableRoadTypes();
            }

            if (AllowableRoadTypes.Contains(stringToCheck))
            {
                returnValue = true;
            }

            return returnValue;
        }

        public static bool CheckIntersectionTypeAllowed(string stringToCheck)
        {
            bool returnValue = false;
            if (AllowableIntersectionTypes.Count == 0)
            {
                PopulateAllowableIntersectionTypes();
            }

            if (AllowableIntersectionTypes.Contains(stringToCheck))
            {
                returnValue = true;
            }

            return returnValue;
        }

        private static List<string> PopulateAllowableRoadTypes()
        {
            List<string> list = new List<string>();
            list.Add("residential");
            list.Add("service");
            list.Add("unclassified");
            list.Add("tertiary");
            list.Add("secondary");
            list.Add("primary");
            list.Add("trunk");
            list.Add("motorway");
            list.Add("motorway_link");
            list.Add("trunk_link");

            return list;
        }

        private static List<string> PopulateAllowableIntersectionTypes()
        {
            List<string> list = new List<string>();
            list.Add("stop");
            list.Add("traffic_signals");

            return list;
        }

        public static Mapsui.Nts.GeometryFeature CreateRoadFeature(GeometryFeature gf, Way way)
        {
            gf["OBJECTID"] = GetOsmId(way);
            gf["STREET"] = GetName(way);
            gf["SPEED_LIMI"] = GetSpeedLimit(way);
            gf["LANES"] = GetLane(way);
            gf["FLOW_DIREC"] = GetDirection(way);

            return gf;
        }

        public static Mapsui.Nts.GeometryFeature CreateIntersectionFeature(GeometryFeature gf, Node node)
        {
            gf["Intersecti"] = GetName(node);
            gf["Intersec_1"] = GetSignalType(node);

            return gf;
        }

        public static string GetSignalType(Node n)
        {
            string returnValue = "All Way Stop";
            if (n.Tags.TryGetValue("highway", out string value))
            {
                if (value == "traffic_signals")
                {
                    returnValue = "Full Signal";
                }
            }

            return returnValue;
        }

        public static string GetName(Node n)
        {
            string returnValue = "Unnamed";
            if (n.Tags.TryGetValue("name", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetOsmId(OsmSharp.Way way)
        {
            string returnValue = "0";

            if (way.Tags.TryGetValue("osm_id", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetName(OsmSharp.Way way)
        {
            string returnValue = "Unnamed";

            if (way.Tags.TryGetValue("name", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetDirection(OsmSharp.Way way)
        {
            string returnValue = "TwoWay";

            if (way.Tags.TryGetValue("oneway", out string value))
            {
                if (value == "yes")
                {
                    returnValue = "FromTo";
                }
            }

            return returnValue;
        }

        public static int GetSpeedLimit(Way way)
        {
            int returnValue = 50;

            if (way.Tags.TryGetValue("maxspeed", out string value))
            {
                if (int.TryParse(value, out int intValue))
                {
                    returnValue = intValue;
                }
            }

            return returnValue;
        }

        public static int GetLane(Way way)
        {
            int returnValue = 2;

            if (way.Tags.TryGetValue("lanes", out string value))
            {
                if (int.TryParse(value, out int intValue))
                {
                    returnValue = intValue;
                }
            }

            return returnValue;
        }
    }
}