using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    public class IntersectionStats
    {
        private double averageTimeSpentHeadingTo;
        private double totalTimeSpentHeadingTo;
        private int numberOfVehiclesEntered;

        public IntersectionStats()
        {
        }

        public void RecordVehicleEntered(float timeSpentEntering)
        {
            numberOfVehiclesEntered++;
            totalTimeSpentHeadingTo += timeSpentEntering;
            averageTimeSpentHeadingTo = totalTimeSpentHeadingTo / numberOfVehiclesEntered;
        }

        public void Reset()
        {
            averageTimeSpentHeadingTo = 0;
            totalTimeSpentHeadingTo = 0;
            numberOfVehiclesEntered = 0;
        }

        public int GetVehiclesEntered()
        {
            return numberOfVehiclesEntered;
        }
    }
}