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
using UrbanEcho.ViewModels;

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
                ExportMapImage();
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

                    MemoryStream? ms = ExportMapImage();
                    if (ms != null)
                    {
                        template.AddVariable("MapImage", ms);
                    }
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

        public static MemoryStream? ExportMapImage()
        {
            MemoryStream? ms = null;
            MainViewModel? mvm = Sim.Sim.GetMainViewModel();
            bool savedFile = false;
            if (mvm != null)
            {
                Map map = mvm.Map.MyMap;
                //https://github.com/Mapsui/Mapsui/blob/98c282bfc8873332c44f551f42f22a7791be5b97/Mapsui.Rendering.Skia/MapRenderer.cs#L87

                //MemoryStream? memoryStream = Mapsui.Rendering.Skia.MapRenderer.RenderToBitmapStream(mvm.Map.MyMap.Navigator.Viewport, mvm.Map.MyMap.Layers,
                //Mapsui.Styles.Color.White);

                //MemoryStream ms = mapRenderer.RenderToBitmapStream(mvm.Map.MyMap);
                MRect? mRect = map.Extent;

                if (mRect == null)
                {
                    mRect = ProjectLayers.TryGetBackgroundExtent();//Fallback to reading background extent if failed to get map extents
                }
                if (mRect != null && double.IsNaN(mRect.Centroid.X))
                {
                    mRect = ProjectLayers.TryGetBackgroundExtent();//Fallback to reading background extent if failed to get map extents
                }
                if (mRect != null && !double.IsNaN(mRect.Centroid.X))//Only create the image if we could get the extents
                {
                    double resolution = Math.Max(mRect.Width / 1024, mRect.Height / 768);
                    Viewport viewport = new Viewport(mRect.Centroid.X, mRect.Centroid.Y, resolution, 0, 1024, 768);
                    Mapsui.Rendering.Skia.MapRenderer mapRenderer = new Mapsui.Rendering.Skia.MapRenderer();

                    ms = mapRenderer.RenderToBitmapStream(viewport, map.Layers, map.RenderService, Mapsui.Styles.Color.White);

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
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Error while exporting image of map {ex.Message}"));
                    }
                }
                if (!savedFile)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Image of map for report was not saved"));
                }
            }

            return ms;
        }
    }
}