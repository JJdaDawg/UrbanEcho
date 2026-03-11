using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Reporting;

namespace UrbanEcho.Models.Report
{
    public class RoadEdgeReportModel
    {
        public string RoadName;
        public string FromRoadName;
        public string ToRoadName;
        public double AverageTimeSpent { get; set; }
        public double TotalTimeSpent { get; set; }
        public double AverageSpeed { get; set; }
        public double AverageWaitTime { get; set; }
        public double TotalWaitTime { get; set; }

        public int VehicleCount { get; set; }

        public RoadEdgeReportModel(string roadName, string fromName, string toName, RecordedStats stats)
        {
            RoadName = roadName;
            FromRoadName = fromName;
            ToRoadName = toName;
            AverageTimeSpent = stats.AverageTimeSpent;
            TotalTimeSpent = stats.TotalTimeSpent;
            AverageSpeed = stats.AverageSpeed;
            AverageWaitTime = stats.AverageWaitTime;
            TotalWaitTime = stats.TotalWaitTime;
            VehicleCount = stats.VehicleCount;
        }
    }
}