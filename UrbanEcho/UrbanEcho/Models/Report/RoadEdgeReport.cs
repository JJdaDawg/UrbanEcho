using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models.Report
{
    /// <summary>
    /// This class provides the structure used in the report for road edges
    /// </summary>
    public class RoadEdgeReport
    {
        public int RoadEdgeReportId { get; set; }
        public List<RoadEdgeReportModel> Roads { get; set; } = new List<RoadEdgeReportModel>();

        public RoadEdgeReport()
        {
        }
    }
}