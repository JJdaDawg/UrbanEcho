using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Reporting;

namespace UrbanEcho.Models.Report
{
    public class IntersectionReportModel
    {
        public string IntersectionName;
        public double AverageTimeSpent { get; private set; }
        public double TotalTimeSpent { get; private set; }
        public double AverageSpeed { get; private set; }
        public double AverageWaitTime { get; private set; }
        public double TotalWaitTime { get; private set; }

        public int NumberOfVehiclesEntered { get; private set; }

        public IntersectionReportModel(string intersectionName, IntersectionStats stats)
        {
            IntersectionName = intersectionName;
            AverageTimeSpent = stats.AverageTimeSpent;
            TotalTimeSpent = stats.TotalTimeSpent;
            AverageSpeed = stats.AverageSpeed;
            AverageWaitTime = stats.AverageWaitTime;
            TotalWaitTime = stats.TotalWaitTime;
            NumberOfVehiclesEntered = stats.NumberOfVehiclesEntered;
        }
    }
}