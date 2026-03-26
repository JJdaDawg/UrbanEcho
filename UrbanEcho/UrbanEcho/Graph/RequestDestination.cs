using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Graph
{
    public class RequestDestination
    {
        public int NodeId { get; }

        public RequestDestination(int nodeId)
        {
            NodeId = nodeId;
        }
    }
}