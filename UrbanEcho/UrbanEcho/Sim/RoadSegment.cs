using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Sim
{
    public class RoadSegment
    {
        public Vector2[] Pos = new Vector2[2];

        public RoadSegment(Vector2 start, Vector2 end)
        {
            Pos[0] = start;
            Pos[1] = end;
        }
    }
}