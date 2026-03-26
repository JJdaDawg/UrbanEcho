using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    public class RecordedStats
    {
        public double AverageTimeSpent { get; private set; }
        public double TotalTimeSpent { get; private set; }
        public double AverageSpeed { get; private set; }
        private double totalSpeed;//Not useful for anywhere else just for calculating average speed

        public double AverageWaitTime { get; private set; }
        public double TotalWaitTime { get; private set; }

        public int VehicleCount { get; private set; }

        public double Lat { get; private set; }

        public double Lon { get; private set; }

        public int Closed { get; private set; }

        public RecordedStats()
        {
        }

        public void RecordVehicle(Stats incomingStats)
        {
            VehicleCount++;

            TotalTimeSpent += incomingStats.ElaspedTime;
            AverageTimeSpent = TotalTimeSpent / VehicleCount;

            totalSpeed += incomingStats.AverageSpeed;
            AverageSpeed = totalSpeed / VehicleCount;

            TotalWaitTime += incomingStats.WaitTime;
            AverageWaitTime = TotalWaitTime / VehicleCount;
        }

        public void SetClosed()//Flag that gets set if closed any time during simulation
        {
            Closed = 1;
        }

        public void SetPosition(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public void Reset()
        {
            AverageTimeSpent = 0;
            TotalTimeSpent = 0;
            AverageSpeed = 0;
            totalSpeed = 0;//Not useful for anywhere else just for calculating average speed
            AverageWaitTime = 0;
            TotalWaitTime = 0;
            VehicleCount = 0;
            Closed = 0;
        }
    }
}