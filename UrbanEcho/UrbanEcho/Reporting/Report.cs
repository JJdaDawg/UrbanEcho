using ClosedXML.Report;
using ClosedXML.Report.Options;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.UI.Avalonia;
using Mapsui.Widgets;
using SkiaSharp;
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
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Reporting
{
    public class Report
    {
        public List<RoadEdgeReportModel> RoadEdgeReport { get; set; } = new List<RoadEdgeReportModel>();

        public IntersectionReport TheReport { get; set; } = new IntersectionReport();

        public static Task? ReportTask { get; set; }
        public static bool TaskStarted { get; set; } = false;

        public Report(List<RoadIntersection> roadIntersections, RoadGraph roadGraph, bool fullReport = false)
        {
            TheReport.Intersections = new List<IntersectionReportModel>();

            RoadEdgeReport = new List<RoadEdgeReportModel>();

            MemoryStream? ms = ExportMapImage();

            foreach (RoadIntersection roadIntersection in roadIntersections)
            {
                if (roadIntersection.EdgesInto.Count == 0) continue;

                List<RoadEdgeReportModel> edges = new List<RoadEdgeReportModel>();
                if (fullReport)
                {
                    foreach (EdgeTrafficRule edgeTrafficRule in roadIntersection.EdgesInto)
                    {
                        RoadEdge roadEdge = edgeTrafficRule.RoadEdge;
                        edges.Add(new RoadEdgeReportModel(roadEdge.Metadata.RoadName, Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "FROM_STREE", "None"),
                        Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "TO_STREET", "None"), roadEdge.GetStats()));
                    }
                    edges.Sort((roadEdgeReport1, roadEdgeReport2) => roadEdgeReport2.VehicleCount.CompareTo(roadEdgeReport1.VehicleCount));
                }
                IntersectionReportModel intersectionReportModel = new IntersectionReportModel(roadIntersection.Name, roadIntersection.GetStats(), edges);

                TheReport.Intersections.Add(intersectionReportModel);
            }

            TheReport.Intersections.Sort((intersectionReport1, intersectionReport2) => intersectionReport2.VehicleCount.CompareTo(intersectionReport1.VehicleCount));

            if (SimManager.Instance.RoadGraph != null)
            {
                foreach (RoadEdge roadEdge in SimManager.Instance.RoadGraph.Edges)
                {
                    RoadEdgeReport.Add(new RoadEdgeReportModel(roadEdge.Metadata.RoadName, Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "FROM_STREE", "None"),
                        Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "TO_STREET", "None"), roadEdge.GetStats()));
                }

                RoadEdgeReport.Sort((roadEdgeReport1, roadEdgeReport2) => roadEdgeReport2.VehicleCount.CompareTo(roadEdgeReport1.VehicleCount));
            }
            if (ReportTask == null || ReportTask.IsCompleted)
            {
                ReportTask = Task.Factory.StartNew(new Action(() => Export(fullReport, ms)), SimManager.Instance.Cts.Token);
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to create a report (report export has not completed a previous export)"));
            }
        }

        private void Export(bool fullReport, MemoryStream? ms)
        {
            //https://github.com/ClosedXML/ClosedXML.Report
            try
            {
                //https://closedxml.io/ClosedXML.Report/docs/en/Flat-tables.html

                //https://github.com/ClosedXML/ClosedXML.Report

                //https://stackoverflow.com/questions/12500091/datetime-tostring-format-that-can-be-used-in-a-filename-or-extension
                string outputFile = @$".\Output\Report-{DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-tt")}.xlsx";
                XLTemplate? template = null;

                if (fullReport)
                {
                    template = new XLTemplate(@".\Resources\Templates\template.xlsx");
                }
                else
                {
                    template = new XLTemplate(@".\Resources\Templates\templateSmall.xlsx");
                }

                template.AddVariable("Date", DateTime.Now.ToString());
                ProjectFile? projectFile = ProjectLayers.GetProject();
                if (projectFile != null)
                {
                    string projectFileName = projectFile.PathForThisFile;
                    template.AddVariable("Project", projectFileName);

                    if (ms != null)
                    {
                        template.AddVariable("MapImage", ms);
                    }
                    template.AddVariable("TheReport", TheReport);
                    template.AddVariable("Roads", RoadEdgeReport);

                    template.Generate();
                }

                template.SaveAs(outputFile);

                //Show report
                Process.Start(new ProcessStartInfo(outputFile) { UseShellExecute = true });

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Report Exported to {outputFile}"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Error while exporting report {ex.Message}"));
            }
        }

        private MemoryStream? ExportMapImage()
        {
            MemoryStream? ms = null;
            MainViewModel? mvm = MainWindow.Instance.GetMainViewModel();
            bool savedFile = false;
            if (mvm != null)
            {
                Map map = mvm.Map.MyMap;
                //https://github.com/Mapsui/Mapsui/blob/98c282bfc8873332c44f551f42f22a7791be5b97/Mapsui.Rendering.Skia/MapRenderer.cs#L87

                MRect? mRect = map.Extent;

                if (mRect == null)
                {
                    mRect = ProjectLayers.TryGetRoadLayerExtent();//Fallback to reading background extent if failed to get map extents
                }
                if (mRect != null && double.IsNaN(mRect.Centroid.X))
                {
                    mRect = ProjectLayers.TryGetRoadLayerExtent();//Fallback to reading background extent if failed to get map extents
                }
                if (mRect != null && !double.IsNaN(mRect.Centroid.X))//Only create the image if we could get the extents
                {
                    double resolution = Math.Max((mRect.Width / 1024), (mRect.Height / 768));
                    Viewport viewport = new Viewport(mRect.Centroid.X, mRect.Centroid.Y, resolution, 0, 1024, 768);
                    Mapsui.Rendering.Skia.MapRenderer mapRenderer = new Mapsui.Rendering.Skia.MapRenderer();

                    ms = mapRenderer.RenderToBitmapStream(viewport, map.Layers, map.RenderService, Mapsui.Styles.Color.White, 1);

                    try
                    {
                        //https://stackoverflow.com/questions/8624071/save-and-load-memorystream-to-from-a-file/19302609
                        using (FileStream file = new FileStream(@$".\Output\Map-{DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-tt")}.png", FileMode.Create, System.IO.FileAccess.Write))
                        {
                            ms.Position = 0;
                            ms.CopyTo(file);
                            savedFile = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Error while exporting image of map {ex.Message}"));
                    }
                }
                if (!savedFile)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Image of map for report was not saved"));
                }
            }

            return ms;
        }
    }
}