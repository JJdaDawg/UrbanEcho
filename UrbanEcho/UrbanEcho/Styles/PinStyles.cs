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
    public class PinStyles
    {
        private Dictionary<string, IStyle> Styles = new Dictionary<string, IStyle>();

        public PinStyles()
        {
            Styles.Add("Default", new VectorStyle { Line = new Pen { Width = 0.25 }, Enabled = false });

            //https://stackoverflow.com/questions/18316683/how-to-get-the-current-project-name-in-c-sharp-code
            string? projectName = Assembly.GetCallingAssembly().GetName().Name;

            if (projectName != null)
            {
                try
                {
                    Styles.Add("Pin", CreateImageStyle(projectName, "Pin.svg"));
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to add pin styles {ex.ToString()}"));
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to get project assembly name while trying to load embedded images"));
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
                    SvgFillColor = Color.PowderBlue
                };
            RelativeOffset relativeOffset = new RelativeOffset();
            relativeOffset.Y = 0.5;
            style.RelativeOffset = relativeOffset;
            return style;
        }

        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Styles/ThemeStyleSample.cs
        public ThemeStyle CreateThemeStyle()
        {
            return new ThemeStyle(f =>
            {
                return Styles["Pin"];
            });
        }
    }
}