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
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using static Mapsui.Rendering.Skia.Functions.ClippingFunctions;

namespace UrbanEcho.Styles
{
    public class VehicleStyles
    {
        private Dictionary<string, IStyle> Styles = new Dictionary<string, IStyle>();

        private Random random = new Random();
        public static int NumberOFCarColors = 20;

        public VehicleStyles()
        {
            Styles.Add("Default", new VectorStyle { Line = new Pen { Width = 0.25 } });
            Styles.Add("Hidden", new VectorStyle { Fill = new Brush(Color.Transparent), Outline = new Pen(Color.Transparent), Enabled = false });

            //https://stackoverflow.com/questions/18316683/how-to-get-the-current-project-name-in-c-sharp-code
            string? projectName = Assembly.GetCallingAssembly().GetName().Name;

            if (projectName != null)
            {
                try
                {
                    for (int i = 0; i < NumberOFCarColors; i++)
                    {
                        Styles.Add($"Car{i}", CreateImageStyle(projectName, "Car.svg", VehicleSettings.CarLength, 48));
                    }
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Unable to add vehicle styles {ex.ToString()}"));
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Unable to get project assembly name while trying to load embedded images"));
            }
        }

        private ImageStyle CreateImageStyle(string projectName, string fileName, float physicalCarLength, int imageWidth)
        {
            string sourceString = $"embedded://{projectName}.Resources.Images.VehicleIcons.{fileName}";

            ImageStyle style = new ImageStyle();
            Random random = new Random();
            style.Image =
                new Image
                {
                    Source = sourceString,
                    SvgFillColor = new Color(random.Next(64, 235), random.Next(64, 235), random.Next(64, 235))
                };

            style.SymbolScale = physicalCarLength / (float)(imageWidth);

            return style;
        }

        private ImageStyle CopyStyle(ImageStyle style)
        {
            ImageStyle newStyle = new ImageStyle();
            newStyle.Image = style.Image;
            try
            {
                newStyle.SymbolScale = style.SymbolScale / Sim.Sim.MyMap.Navigator.Viewport.Resolution;
            }
            catch
            {
                newStyle.SymbolScale = 1.25f;
            }
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
                            string? carType = f["VehicleType"]?.ToString();
                            if (carType != null)
                            {
                                if (Styles.ContainsKey(carType))
                                {
                                    ImageStyle style = CopyStyle((ImageStyle)Styles[carType]);

                                    float angle = -(float)f["Angle"];
                                    style.SymbolRotation = angle;
                                    return style;
                                }
                            }
                        }
                    }
                    catch
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Tried to show vehicle style that does not exist"));
                    }
                }
                return null;
            });
        }
    }
}