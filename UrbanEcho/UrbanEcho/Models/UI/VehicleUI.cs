using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;

namespace UrbanEcho.Models.UI
{
    public class VehicleUI
    {
        public int Id { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public float Kmh { get; set; }
        public float SpeedLimit { get; set; }
        public VehicleStates State { get; set; }
        public bool IsWaiting { get; set; }
        public bool WaitingOnIntersection { get; set; }
        public bool VehicleInFront { get; set; }
        public float MetersFromCarInFront { get; set; }
        public string RoadName { get; set; } = string.Empty;
    }
}