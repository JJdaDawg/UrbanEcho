using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.Messages
{
    public class MapFeatureSelectedMessage
    {
        public MapFeatureType Type { get; }
        public object Data { get; }

        public MapFeatureSelectedMessage(MapFeatureType type, object data)
        {
            Type = type;
            Data = data;
        }
    }

    public class MapFeatureDeselectedMessage { }
}
