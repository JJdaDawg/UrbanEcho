using Box2dNet.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace UrbanEcho.Sim
{
    public class RoadIntersection
    {
        public List<ConnectionData> Connections;

        public Vector2 Center;

        public IntersectionBody? Body;

        public string Name = "test123";

        public float WaitTime = 5.0f;

        public RoadIntersection(string name, float waitTime)
        {
            this.Name = name;
            Connections = new List<ConnectionData>();
            WaitTime = waitTime;
        }

        public void Init()
        {
            if (Connections?.Count > 0)
            {
                int index = Connections[0].IndexValue;
                Center = Connections[0].RoadSegment.Pos[index];

                Body = new IntersectionBody(this);
            }
        }
    }
}