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
    public class VehicleStyles
    {
        private Dictionary<string, IStyle> Styles = new Dictionary<string, IStyle>();

        private Random random = new Random();

        public VehicleStyles()
        {
            Styles.Add("Default", new VectorStyle { Line = new Pen { Width = 0.25 } });
            Styles.Add("Hidden", new VectorStyle { Fill = new Brush(Color.Transparent), Outline = new Pen(Color.Transparent) });
            //https://stackoverflow.com/questions/18316683/how-to-get-the-current-project-name-in-c-sharp-code
            string? projectName = Assembly.GetCallingAssembly().GetName().Name;

            if (projectName != null)
            {
                try
                {
                    Styles.Add("RedCar", CreateImageStyle(projectName, "RedCar.png"));
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Unable to add vehicle styles {ex.ToString()}"));
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Unable to get project assembly name while trying to load embedded images"));
            }
        }

        private ImageStyle CreateImageStyle(string projectName, string fileName)
        {
            string sourceString = $"embedded://{projectName}.Resources.Images.VehicleIcons.{fileName}";

            ImageStyle style = new ImageStyle();

            style.Image =
                new Image
                {
                    Source = sourceString,
                };

            style.SymbolScale = 1.25f;

            return style;
        }

        private ImageStyle CopyStyle(ImageStyle style)
        {
            ImageStyle newStyle = new ImageStyle();
            newStyle.Image = style.Image;
            newStyle.SymbolScale = style.SymbolScale;
            return newStyle;
        }

        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Styles/ThemeStyleSample.cs
        public ThemeStyle CreateThemeStyle()
        {
            return new ThemeStyle(f =>
            {
                if (f is Mapsui.Layers.PointFeature geometryFeature)
                {
                    try
                    {
                        if (f["Hidden"]?.ToString() == "true")
                        {
                            return Styles["Hidden"];
                        }
                        else
                        {
                            switch (f["VehicleType"]?.ToString())
                            {
                                case "RedCar":

                                    ImageStyle style = CopyStyle((ImageStyle)Styles["RedCar"]);
                                    style.SymbolRotation = random.NextDouble() * 360.0f;
                                    return style;

                                default:

                                    return Styles["Default"];
                            }
                        }
                    }
                    catch
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Simulation.GetMainViewModel(), $"Tried to show vehicle style that does not exist"));
                    }
                }
                return null;
            });
        }
    }
}