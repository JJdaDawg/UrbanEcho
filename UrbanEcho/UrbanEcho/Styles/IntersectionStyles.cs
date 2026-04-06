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
using static Mapsui.Rendering.Skia.Functions.ClippingFunctions;

namespace UrbanEcho.Styles
{
    /// <summary>
    /// Intersection Styles for displaying intersections
    /// </summary>
    public class IntersectionStyles
    {
        private Dictionary<string, IStyle> Styles = new Dictionary<string, IStyle>();

        public IntersectionStyles()
        {
            Styles.Add("Default", new VectorStyle { Line = new Pen { Width = 0.25 }, Enabled = false });

            //https://stackoverflow.com/questions/18316683/how-to-get-the-current-project-name-in-c-sharp-code
            string? projectName = Assembly.GetCallingAssembly().GetName().Name;

            if (projectName != null)
            {
                try
                {
                    Styles.Add("TrafficLight", CreateImageStyle(projectName, "TrafficLight.png"));
                    Styles.Add("StopSign", CreateImageStyle(projectName, "StopSign.png"));
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to add intersection styles {ex.ToString()}"));
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to get project assembly name while trying to load embedded images"));
            }
        }

        /// <summary>
        /// Creates a Image style using the filename given
        /// </summary>
        private ImageStyle CreateImageStyle(string projectName, string fileName)
        {
            string sourceString = $"embedded://{projectName}.Resources.Images.TrafficIcons.{fileName}";

            ImageStyle style = new ImageStyle();

            style.Image =
                new Image
                {
                    Source = sourceString,
                };

            return style;
        }

        /// <summary>
        /// Creates a theme style type of style that can be shown differently depending
        /// on features displayed
        /// </summary>
        /// <returns>Returns a <see cref="ThemeStyle"/> </returns>
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
                        switch (f["Intersec_1"]?.ToString())
                        {
                            case "Two Way Stop":
                                return Styles["StopSign"];

                            case "All Way Stop":
                                return Styles["StopSign"];

                            case "Full Signal":
                                return Styles["TrafficLight"];

                            default:

                                return Styles["Default"];
                        }
                    }
                    catch
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Tried to show intersection style that does not exist"));
                    }
                }
                return null;
            });
        }
    }
}