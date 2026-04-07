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
using System.Threading;
using System.Threading.Tasks;

using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Models;
using UrbanEcho.Models.Report;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Reporting
{
    /// <summary>
    /// Class used for creating a report
    /// </summary>
    public class ReportTask
    {
        public RoadEdgeReport TheRoadEdgeReport { get; set; } = new RoadEdgeReport();

        public IntersectionReport TheIntersectionReport { get; set; } = new IntersectionReport();

        public static Task? ExportTask { get; set; }
        public static bool TaskStarted { get; set; } = false;

        /// <summary>
        /// Creates a report, exports it to a excel sheet and updates the database
        /// </summary>
        public ReportTask(List<RoadIntersection> roadIntersections, RoadGraph roadGraph)
        {
            TheIntersectionReport.Intersections = new List<IntersectionReportModel>();

            TheRoadEdgeReport.Roads = new List<RoadEdgeReportModel>();

            DateTime dateTime = DateTime.Now;
            MemoryStream? ms = null;
            try
            {
                ms = ExportMapImage(dateTime);
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to export image for report {ex.Message}"));
            }
            foreach (RoadIntersection roadIntersection in roadIntersections)
            {
                if (roadIntersection.EdgesInto.Count == 0) continue;

                List<RoadEdgeReportModel> edges = new List<RoadEdgeReportModel>();

                IntersectionReportModel intersectionReportModel = new IntersectionReportModel(roadIntersection.Name, roadIntersection.GetStats(), edges);

                TheIntersectionReport.Intersections.Add(intersectionReportModel);
            }

            TheIntersectionReport.Intersections.Sort((intersectionReport1, intersectionReport2) => intersectionReport2.VehicleCount.CompareTo(intersectionReport1.VehicleCount));

            if (SimManager.Instance.RoadGraph != null)
            {
                foreach (RoadEdge roadEdge in SimManager.Instance.RoadGraph.Edges)
                {
                    if (roadEdge.IsFromStartOfLineString)
                    {
                        TheRoadEdgeReport.Roads.Add(new RoadEdgeReportModel(roadEdge.Metadata.RoadName, Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "FROM_STREE", ""),
                        Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "TO_STREET", ""), roadEdge.GetStats()));
                    }
                    else
                    {
                        TheRoadEdgeReport.Roads.Add(new RoadEdgeReportModel(roadEdge.Metadata.RoadName, Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "TO_STREET", ""),
                        Helpers.Helper.TryGetFeatureKVPToString(roadEdge.Feature, "FROM_STREE", ""), roadEdge.GetStats()));
                    }
                }

                TheRoadEdgeReport.Roads.Sort((roadEdgeReport1, roadEdgeReport2) => roadEdgeReport2.VehicleCount.CompareTo(roadEdgeReport1.VehicleCount));
            }
            if (ExportTask == null || ExportTask.IsCompleted)
            {
                try
                {
                    ExportTask = Task.Factory.StartNew(new Action(() => Export(ms, dateTime)), SimManager.Instance.Cts.Token);
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to export report {ex.Message}"));
                }

                string reportName = Path.GetFileNameWithoutExtension(ProjectLayers.GetProject()?.PathForThisFile ?? "UnNamedProject.uep");

                bool anyRoadsClosed = CheckAnyRoadsClosed(TheRoadEdgeReport.Roads);

                if (anyRoadsClosed)
                {
                    reportName = reportName + " (Closures)";
                }

                Report report = new Report(dateTime, reportName, TheRoadEdgeReport, TheIntersectionReport);
                try
                {
                    ReportContext reportContext = new ReportContext();

                    reportContext.Add(report);
                    Task saveToDatabase = reportContext.SaveChangesAsync(SimManager.Instance.Cts.Token);
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to add report to database {ex.Message}"));
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to create a report (report export has not completed a previous export)"));
            }
        }

        /// <summary>
        /// Checks if any roads were closed during the report
        /// </summary>
        private bool CheckAnyRoadsClosed(List<RoadEdgeReportModel> roadEdges)
        {
            bool returnValue = false;

            if (roadEdges.Any(e => e.Closed > 0))
            {
                returnValue = true;
            }

            return returnValue;
        }

        /// <summary>
        /// Exports the report to a excel file
        /// </summary>
        private void Export(MemoryStream? ms, DateTime dateTime)
        {
            //https://github.com/ClosedXML/ClosedXML.Report
            try
            {
                //https://closedxml.io/ClosedXML.Report/docs/en/Flat-tables.html

                //https://github.com/ClosedXML/ClosedXML.Report

                //https://stackoverflow.com/questions/12500091/datetime-tostring-format-that-can-be-used-in-a-filename-or-extension
                string outputFile = @$".\Output\Report-{dateTime.ToString("MM-dd-yyyy_hh-mm-ss-tt")}.xlsx";
                XLTemplate? template = null;

                template = new XLTemplate(@".\Resources\Templates\template.xlsx");

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
                    template.AddVariable("Intersections", TheIntersectionReport.Intersections);
                    template.AddVariable("Roads", TheRoadEdgeReport.Roads);

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

        /// <summary>
        /// Gets a memory stream that represents a picture of the map for display on the report
        /// </summary>
        /// <returns>Returns a image as a <see cref="MemoryStream"/> </returns>
        private MemoryStream? ExportMapImage(DateTime dateTime)
        {
            MemoryStream? ms = null;
            MainViewModel? mvm = MainWindow.Instance.GetMainViewModel();
            bool savedFile = false;
            if (mvm != null)
            {
                Map map = mvm.Map.MyMap;
                //https://github.com/Mapsui/Mapsui/blob/98c282bfc8873332c44f551f42f22a7791be5b97/Mapsui.Rendering.Skia/MapRenderer.cs#L87

                LayerCollection layers = new LayerCollection();
                Thread.Sleep(3000);//Give time for map to zoom out so export image looks correct
                foreach (ILayer layer in map.Layers)
                {
                    while (layer.Busy && (layer.Name == "background" || layer.Name == "Roads"))
                    {
                        Thread.Sleep(100);//Wait until background and roads layer is not busy
                    }

                    if (layer.Name == "background" || layer.Name == "Roads")
                    {
                        layers.Add(layer);
                    }
                }

                MRect? extent = ProjectLayers.TryGetRoadLayerExtent();

                if (extent != null)
                {
                    double centerX = extent.MinX + (extent.MaxX - extent.MinX) / 2;
                    double centerY = extent.MinY + (extent.MaxY - extent.MinY) / 2;

                    double resolution = Math.Max(extent.Width / 1024, extent.Height / 768);
                    Viewport viewport = new Viewport(centerX, centerY, resolution, 0, 1024, 768);

                    try
                    {
                        Mapsui.Rendering.Skia.MapRenderer mapRenderer = new Mapsui.Rendering.Skia.MapRenderer();
                        ms = mapRenderer.RenderToBitmapStream(viewport, layers, map.RenderService, Mapsui.Styles.Color.White, 1);
                        //https://stackoverflow.com/questions/8624071/save-and-load-memorystream-to-from-a-file/19302609
                        using (FileStream file = new FileStream(@$".\Output\Map-{dateTime.ToString("MM-dd-yyyy_hh-mm-ss-tt")}.png", FileMode.Create, System.IO.FileAccess.Write))
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