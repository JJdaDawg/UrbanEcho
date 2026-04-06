using Mapsui;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using System;
using System.Linq;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Styles;

namespace UrbanEcho.Sim
{
    /// <summary>
    /// Setups the map CRS (Coordinate system) and what widgets should be shown
    /// </summary>
    public class SetupMap
    {
        public static void Init(Map MyMap)
        {
            MyMap.CRS = "EPSG:3857"; // The Map CRS needs to be set

            //Add default Zoom limit right away so no crashes if mouse wheel scrolling without layer loaded
            ProjectLayers.SetDefaultZoomLimit(MyMap);
            MyMap.BackColor = Color.Black;
            //Removes debug info on mapControl

            LoggingWidget.ShowLoggingInMap = ActiveMode.No;

            try
            {
                PerformanceWidget? performanceWidget = MyMap.Widgets.OfType<PerformanceWidget>().FirstOrDefault();
                if (performanceWidget != null)
                {
                    performanceWidget.Enabled = false;//Removes fps info on mapControl
                }
            }
            catch (System.Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to remove performance Widget {ex.ToString()}"));
            }
        }
    }
}