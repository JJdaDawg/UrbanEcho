using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Sim;
using static Mapsui.Rendering.Skia.Functions.ClippingFunctions;

namespace UrbanEcho.Styles
{
    public class IntersectionStyles
    {
        private Dictionary<string, IStyle> Styles = new Dictionary<string, IStyle>();

        public IntersectionStyles()
        {
            Styles.Add("Default", new VectorStyle { Line = new Pen { Width = 0.25 } });

            //https://stackoverflow.com/questions/18316683/how-to-get-the-current-project-name-in-c-sharp-code
            string? projectName = Assembly.GetCallingAssembly().GetName().Name;

            if (projectName != null)
            {
                try
                {
                    Styles.Add("TrafficLight", CreateImageStyle(projectName, "TrafficLight.png"));
                    Styles.Add("StopSign", CreateImageStyle(projectName, "StopSign.png"));
                    Styles.Add("Flasher", CreateImageStyle(projectName, "Flasher.png"));
                    Styles.Add("Pedestrian", CreateImageStyle(projectName, "Pedestrian.png"));
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Unable to add intersection styles {ex.ToString()}"));
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Unable to get project assembly name while trying to load embedded images"));
            }
        }

        private ImageStyle CreateImageStyle(string projectName, string fileName)
        {
            string sourceString = $"embedded://{projectName}.Resources.Images.TrafficIcons.{fileName}";

            ImageStyle style = new ImageStyle();

            style.Image =
                new Image
                {
                    Source = sourceString,
                };

            style.SymbolScale = 1.25f;

            return style;
        }

        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Styles/ThemeStyleSample.cs
        public ThemeStyle CreateThemeStyle()
        {
            return new ThemeStyle(f =>
            {
                if (f is GeometryFeature geometryFeature)
                {
                    if (!(geometryFeature.Geometry is Point))
                        return null;

                    try
                    {
                        //TODO: make Intersec_1 (intersection type) not hardcoded and each case can't be hardcoded
                        //      maybe change to for loop so we can compare against a list of strings
                        switch (f["Intersec_1"]?.ToString())
                        {
                            case "Two Way Stop":
                                return Styles["StopSign"];

                            case "All Way Stop":
                                return Styles["StopSign"];

                            case "Flasher":
                                return Styles["Flasher"];

                            case "Full Signal":
                                return Styles["TrafficLight"];

                            case "Intersection Pedestrian Signal":
                                return Styles["Pedestrian"];

                            case "Pedestrian Crossover":
                                return Styles["Pedestrian"];

                            case "Stop with LRT Signals":
                                return Styles["TrafficLight"];

                            default:

                                return Styles["Default"];
                        }
                    }
                    catch
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Tried to show intersection style that does not exist"));
                    }
                }
                return null;
            });
        }
    }
}