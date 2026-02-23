using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Sim
{
    public class ConnectionData
    {
        public RoadSegment RoadSegment;
        public int IndexValue;

        public ConnectionData(RoadSegment roadSegment, int indexValue)
        {
            RoadSegment = roadSegment;
            IndexValue = indexValue;
        }

        public ConnectionData(RoadSegment roadSegment)
        {
            RoadSegment = roadSegment;
            IndexValue = 0;
        }
    }
}