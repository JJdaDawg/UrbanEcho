using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models.Report
{
    public class IntersectionReport
    {
        public List<IntersectionReportModel> Intersections { get; set; } = new List<IntersectionReportModel>();
        public string Title { get; set; } = "The Title";//string that isn't used to avoid parsing problems

        public IntersectionReport()
        {
        }
    }
}