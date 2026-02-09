using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using static Mapsui.Rendering.Skia.Functions.ClippingFunctions;

namespace UrbanEcho
{
    public class IntersectionStyles
    {
        private Dictionary<String, IStyle> Styles = new Dictionary<String, IStyle>();

        public IntersectionStyles()
        {
            Styles.Add("Default", new VectorStyle { Line = new Pen { Width = 0.25 } });

            string projectName = "UrbanEcho";

            Styles.Add("TrafficLight", CreateImageStyle(projectName, "TrafficLight.png"));
            Styles.Add("StopSign", CreateImageStyle(projectName, "StopSign.png"));
            Styles.Add("Flasher", CreateImageStyle(projectName, "Flasher.png"));
            Styles.Add("Pedestrian", CreateImageStyle(projectName, "Pedestrian.png"));
        }

        private ImageStyle CreateImageStyle(string projectName, string fileName)
        {
            string sourceString = $"embedded://{projectName}.Resources.Images.TrafficIcons.{fileName}";

            ImageStyle style = new ImageStyle();

            style.Image =
                new Mapsui.Styles.Image
                {
                    Source = sourceString,
                };

            style.SymbolScale = 1;

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
                                return Styles["Pedistrian"];

                            case "Stop with LRT Signals":
                                return Styles["TrafficLight"];

                            default:

                                return Styles["Default"];
                        }
                    }
                    catch
                    {
                        //TO DO
                        //add error here
                    }
                }
                return null;
            });
        }
    }
}