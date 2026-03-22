using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models.Report
{
    public class Report
    {
        public int ReportId { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
        public string Name { get; set; }

        public RoadEdgeReport RoadEdgeReport { get; set; }

        public IntersectionReport IntersectionReport { get; set; }

        private Report()//Unused only for creation of database
        {
        }

        public Report(DateTime dateTime, string name, RoadEdgeReport roadEdgeReport, IntersectionReport intersectionReport)
        {
            DateTime = dateTime;
            Name = name;
            RoadEdgeReport = roadEdgeReport;
            IntersectionReport = intersectionReport;
        }
    }
}