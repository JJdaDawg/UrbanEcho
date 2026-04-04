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
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Events.UI;
using static UrbanEcho.FileManagement.FileTypes;

namespace UrbanEcho.FileManagement
{
    /// <summary>
    /// Class used for loading osm data from the viewport
    /// </summary>
    public class OsmData
    {
        private readonly double maxResolution = 7.5;//Sets how big a viewport window can be used, if zoomed too far out a warning is shown

        public OsmData()
        {
        }

        /// <summary>
        /// Begins the import of the osm data from the viewport
        /// </summary>
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

        /// <summary>
        /// Downloads the osm data from the viewport using the overpass API, saves it to a file and then
        /// creates a event so that the new road and intersection layer is loaded to the project
        /// </summary>
        private async Task DownloadFile(double minLat, double minLon, double maxLat, double maxLon)
        {
            bool fileSavedNoExceptions = false;
            int numberOfRetries = 3;
            int numberOfTries = 0;

            while (fileSavedNoExceptions == false && numberOfTries < numberOfRetries)
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
                        fileSavedNoExceptions = true;
                    }
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Tried {numberOfTries + 1} of {numberOfRetries} tries Error getting data for viewport {ex.Message}"));
                }
                numberOfTries++;
                if (!fileSavedNoExceptions)
                {
                    if (numberOfTries < numberOfRetries)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Retrying in three seconds"));
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Max Retries reached failed to get data"));
                    }
                }
            }
        }
    }
}