using DocumentFormat.OpenXml.Spreadsheet;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Events.UI;
using static UrbanEcho.FileManagement.FileTypes;

namespace UrbanEcho.FileManagement
{
    public class OsmData
    {
        private readonly double maxResolution = 7.5;

        public OsmData()
        {
        }

        public void StartImport()
        {
            Viewport viewport = MainWindow.Instance.GetMap().Navigator.Viewport;
            MRect extents = viewport.ToExtent();

            (double minLon, double minLat) = SphericalMercator.ToLonLat(extents.Left, extents.Bottom);
            (double maxLon, double maxLat) = SphericalMercator.ToLonLat(extents.Right, extents.Top);

            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Current Viewport resolution {viewport.Resolution}, Area Requested minlat:{minLat},minlon:{minLon},maxlat:{maxLat},maxlon:{maxLon}"));
            if (viewport.Resolution > maxResolution)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Zoomed out too far for importing data"));
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Downloading import data"));

                Task task = DownloadFile(minLat, minLon, maxLat, maxLon);
            }
        }

        private async Task DownloadFile(double minLat, double minLon, double maxLat, double maxLon)
        {
            try
            {
                HttpClient client = new HttpClient();

                string query = @$"
                                  [bbox:{minLat},{minLon},{maxLat},{maxLon}]
                                  [out:xml]
                                  [timeout:90];
                                  way({minLat},{minLon},{maxLat},{maxLon})[highway~""residential|unclassified|tertiary|secondary|primary|trunk|motorway|motorway_link|trunk_link""];
                                  out meta;
                                    node(w);
                                    out meta;
node({minLat},{minLon},{maxLat},{maxLon})[highway~""stop|traffic_signals""];
out meta;
                            ";
                string url = "https://overpass-api.de/api/interpreter";
                string returnValue = string.Empty;

                StringContent content = new StringContent($"data= {Uri.EscapeDataString(query)}", Encoding.UTF8, "application/x-www-form-urlencoded");

                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                returnValue = await response.Content.ReadAsStringAsync();

                if (returnValue != string.Empty)
                {
                    Guid guid = Guid.NewGuid();
                    string fileName = $@"{guid}.osm";
                    string directory = @$".\Downloads";
                    string filePath = @$"{directory}\{fileName}";

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(filePath, returnValue);

                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Saved downloaded data to {filePath}"));
                    LoadFileEvent loadRoads = new LoadFileEvent(FileType.RoadLayerFile, filePath, MainWindow.Instance.GetMap());
                    EventQueueForSim.Instance.Add(loadRoads);
                    LoadFileEvent loadIntersections = new LoadFileEvent(FileType.IntersectionLayerFile, filePath, MainWindow.Instance.GetMap());
                    EventQueueForSim.Instance.Add(loadIntersections);
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Error getting data for viewport {ex.Message}"));
            }
        }
    }
}