using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Reporting;

namespace UrbanEcho.Models.Report
{
    /// <summary>
    /// This class provides the values used in the report for each intersection
    /// </summary>
    public class IntersectionReportModel
    {
        public int IntersectionReportModelId { get; set; }
        public string IntersectionName { get; set; } = "";
        public double AverageTimeSpent { get; set; }
        public double TotalTimeSpent { get; set; }
        public double AverageSpeed { get; set; }
        public double AverageWaitTime { get; set; }
        public double TotalWaitTime { get; set; }

        public int VehicleCount { get; set; }

        public List<RoadEdgeReportModel> Edges { get; set; } = new List<RoadEdgeReportModel>();

        public double Lat { get; private set; }

        public double Lon { get; private set; }

        private IntersectionReportModel() //For database creation not used otherwise
        {
        }

        public IntersectionReportModel(string intersectionName, RecordedStats stats, List<RoadEdgeReportModel> edges)
        {
            Edges = edges;
            IntersectionName = intersectionName;
            AverageTimeSpent = stats.AverageTimeSpent;
            TotalTimeSpent = stats.TotalTimeSpent;
            AverageSpeed = stats.AverageSpeed;
            AverageWaitTime = stats.AverageWaitTime;
            TotalWaitTime = stats.TotalWaitTime;
            VehicleCount = stats.VehicleCount;
            Lat = stats.Lat;
            Lon = stats.Lon;
        }
    }
}