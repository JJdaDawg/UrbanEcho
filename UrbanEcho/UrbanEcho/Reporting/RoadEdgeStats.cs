using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    public class RoadEdgeStats
    {
        public double AverageTimeSpent { get; private set; }
        public double TotalTimeSpent { get; private set; }
        public double AverageSpeed { get; private set; }
        private double totalSpeed;//Not useful for anywhere else just for calculating average speed

        public double AverageWaitTime { get; private set; }
        public double TotalWaitTime { get; private set; }

        public int NumberOfVehiclesExited { get; private set; }

        public RoadEdgeStats()
        {
        }

        public void RecordVehicleExited(Stats incomingStats)
        {
            NumberOfVehiclesExited++;

            TotalTimeSpent += incomingStats.ElaspedTime;
            AverageTimeSpent = TotalTimeSpent / NumberOfVehiclesExited;

            totalSpeed += incomingStats.AverageSpeed;
            AverageSpeed = totalSpeed / NumberOfVehiclesExited;

            TotalWaitTime += incomingStats.WaitTime;
            AverageWaitTime = TotalWaitTime / NumberOfVehiclesExited;
        }

        public void Reset()
        {
            AverageTimeSpent = 0;
            TotalTimeSpent = 0;
            AverageSpeed = 0;
            totalSpeed = 0;//Not useful for anywhere else just for calculating average speed
            AverageWaitTime = 0;
            TotalWaitTime = 0;
            NumberOfVehiclesExited = 0;
        }
    }
}