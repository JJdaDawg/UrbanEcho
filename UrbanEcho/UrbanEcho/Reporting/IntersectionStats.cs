using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    public class IntersectionStats
    {
        public double AverageTimeSpent { get; private set; }
        public double TotalTimeSpent { get; private set; }
        public double AverageSpeed { get; private set; }
        private double totalSpeed;//Not useful for anywhere else just for calculating average speed

        public double AverageWaitTime { get; private set; }
        public double TotalWaitTime { get; private set; }

        public int NumberOfVehiclesEntered { get; private set; }

        public IntersectionStats()
        {
        }

        public void RecordVehicleEntered(Stats incomingStats)
        {
            NumberOfVehiclesEntered++;

            TotalTimeSpent += incomingStats.ElaspedTime;
            AverageTimeSpent = TotalTimeSpent / NumberOfVehiclesEntered;

            totalSpeed += incomingStats.AverageSpeed;
            AverageSpeed = totalSpeed / NumberOfVehiclesEntered;

            TotalWaitTime += incomingStats.WaitTime;
            AverageWaitTime = TotalWaitTime / NumberOfVehiclesEntered;
        }

        public void Reset()
        {
            AverageTimeSpent = 0;
            TotalTimeSpent = 0;
            AverageSpeed = 0;
            totalSpeed = 0;//Not useful for anywhere else just for calculating average speed
            AverageWaitTime = 0;
            TotalWaitTime = 0;
            NumberOfVehiclesEntered = 0;
        }
    }
}