using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models.UI;

namespace UrbanEcho.Models
{
    public class VehicleReadOnly
    {
        private Vehicle v;

        public VehicleReadOnly(Vehicle v)
        {
            this.v = v;
        }

        public int Id()
        {
            return v.Id;
        }

        public string VehicleType()
        {
            return v.VehicleType;
        }

        public float Kmh()
        {
            return v.Kmh;
        }

        public float SpeedLimit()
        {
            return v.SpeedLimit;
        }

        public VehicleStates State()
        {
            return v.State;
        }

        public bool IsWaiting()
        {
            return v.IsWaiting;
        }

        public bool WaitingOnIntersection()
        {
            return v.WaitingOnIntersection;
        }

        public bool VehicleInFront()
        {
            return v.VehicleInFront;
        }

        public float MetersFromCarInFront()
        {
            return v.MetersFromCarInFront;
        }

        public string RoadName()
        {
            return v.RoadName;
        }

        public bool IsForceStopped()
        {
            return v.IsForceStopped;
        }

        public Vector2 Pos()
        {
            return v.Pos;
        }

        public bool InstanceMatches(Vehicle vehicle)
        {
            return (v == vehicle);
        }

        public IReadOnlyList<Mapsui.IFeature> GetRemainingPathFeatures()
        {
            return v.GetRemainingPathFeatures();
        }
    }
}