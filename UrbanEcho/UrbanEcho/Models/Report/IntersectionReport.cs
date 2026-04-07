using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models.Report
{
    /// <summary>
    /// This class provides the structure for the intersection portion of the report
    /// </summary>
    public class IntersectionReport
    {
        public int IntersectionReportId { get; set; }
        public List<IntersectionReportModel> Intersections { get; set; } = new List<IntersectionReportModel>();

        public IntersectionReport()
        {
        }
    }
}