using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    public class RoadEdgeStats
    {
        private double averageTimeSpentHeadingTo;
        private double totalTimeSpentHeadingTo;
        private int numberOfVehiclesExited;

        public RoadEdgeStats()
        {
        }

        public void RecordVehicleExited(float timeSpentEntering)
        {
            numberOfVehiclesExited++;
            totalTimeSpentHeadingTo += timeSpentEntering;
            averageTimeSpentHeadingTo = totalTimeSpentHeadingTo / numberOfVehiclesExited;
        }

        public void Reset()
        {
            averageTimeSpentHeadingTo = 0;
            totalTimeSpentHeadingTo = 0;
            numberOfVehiclesExited = 0;
        }

        public int GetVehiclesExited()
        {
            return numberOfVehiclesExited;
        }
    }
}