using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Reporting
{
    public class Stats
    {
        public double ElaspedTime;
        public double WaitTime;
        public double AverageSpeed;

        public Stats()
        {
        }

        public void Reset()
        {
            ElaspedTime = 0;
            WaitTime = 0;
            AverageSpeed = 0;
        }
    }
}