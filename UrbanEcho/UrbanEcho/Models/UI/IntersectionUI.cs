using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models.UI
{
    public class IntersectionUI
    {
        public int Id { get; set; }
        public string GeoId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Municipality { get; set; } = string.Empty;
        public string OwnedBy { get; set; } = string.Empty;
        public string MaintainedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> ConnectingRoads { get; set; } = new();
    }
}
