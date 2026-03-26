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
        public int RoadEdgeReportModelId { get; set; }

        public string RoadName;
        public string FromRoadName;
        public string ToRoadName;
        public double AverageTimeSpent { get; set; }
        public double TotalTimeSpent { get; set; }
        public double AverageSpeed { get; set; }
        public double AverageWaitTime { get; set; }
        public double TotalWaitTime { get; set; }

        public int VehicleCount { get; set; }

        public double Lat { get; private set; }

        public double Lon { get; private set; }

        public int Closed { get; private set; } //0 not Closed, non zero as closed

        private RoadEdgeReportModel()//Unused only for creation of database
        {
        }

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
            Lat = stats.Lat;
            Lon = stats.Lon;
            Closed = stats.Closed;
        }
    }
}