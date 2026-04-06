using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    /// <summary>
    /// Stats that are recorded when a vehicle exits a road
    /// </summary>
    public class Stats
    {
        public double ElaspedTime;
        public double WaitTime;
        public double AverageSpeed;

        public Stats()
        {
        }

        /// <summary>
        /// Resets the stats
        /// </summary>
        public void Reset()
        {
            ElaspedTime = 0;
            WaitTime = 0;
            AverageSpeed = 0;
        }
    }
}