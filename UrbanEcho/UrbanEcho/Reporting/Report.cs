using ClosedXML.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Models;
using UrbanEcho.Models.Report;

namespace UrbanEcho.Reporting
{
    public static class Report
    {
        public static void Export(List<RoadIntersection> roadIntersections, RoadGraph roadGraph)
        {
            List<IntersectionReportModel> intersectionReportModels = new List<IntersectionReportModel>();
            List<RoadEdgeReportModel> roadEdgeReportModels = new List<RoadEdgeReportModel>();

            foreach (RoadIntersection roadIntersection in roadIntersections)
            {
                intersectionReportModels.Add(new IntersectionReportModel(roadIntersection.Name, roadIntersection.GetStats()));
            }
            if (Sim.Sim.RoadGraph != null)
            {
                foreach (RoadEdge roadEdge in Sim.Sim.RoadGraph.Edges)
                {
                    roadEdgeReportModels.Add(new RoadEdgeReportModel(roadEdge.Metadata.RoadName, Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "FROM_STREE", "None"),
                        Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "TO_STREET", "None"), roadEdge.GetStats()));
                }
            }
            //https://github.com/ClosedXML/ClosedXML.Report
            try
            {
                //https://closedxml.io/ClosedXML.Report/docs/en/Flat-tables.html

                //https://github.com/ClosedXML/ClosedXML.Report

                //https://stackoverflow.com/questions/12500091/datetime-tostring-format-that-can-be-used-in-a-filename-or-extension
                string outputFile = @$".\Output\Report-{DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-tt")}.xlsx";
                var template = new XLTemplate(@".\Resources\Templates\template.xlsx");

                template.AddVariable("Date", DateTime.Now.ToString());
                ProjectFile? projectFile = ProjectLayers.GetProject();
                if (projectFile != null)
                {
                    string projectFileName = projectFile.PathForThisFile;
                    template.AddVariable("Project", projectFileName);
                    template.AddVariable("Intersections", intersectionReportModels);
                    template.AddVariable("Roads", roadEdgeReportModels);
                    template.Generate();
                }
                /*
                foreach (RoadIntersection r in roadIntersections)
                {
                    template.AddVariable(r.Name);
                    template.Generate();
                    break;
                }*/

                template.SaveAs(outputFile);

                //Show report
                Process.Start(new ProcessStartInfo(outputFile) { UseShellExecute = true });

                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Report Exported to {outputFile}"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Error while exporting report {ex.Message}"));
            }
        }
    }
}