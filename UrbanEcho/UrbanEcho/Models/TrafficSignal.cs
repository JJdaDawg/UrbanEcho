using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models
{
    public class TrafficSignal
    {
        public enum SignalType
        {
            Light,
            StopSign,
            YieldSign
        }

        public enum LightStatus
        {
            Red,
            Green,
            ExtendedGreen,
            Yellow
        }
    }
}
