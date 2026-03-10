using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Styles
{
    public class RoadLabelStyles
    {
        private LabelStyle CreateLabelStyle(GeometryFeature gf)
        {
            LabelStyle style = new LabelStyle();

            style.Text = Helpers.Helper.TryGetFeatureKVPToString(gf, "STREET", "Unnamed Road");

            return style;
        }

        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Styles/ThemeStyleSample.cs
        public ThemeStyle CreateThemeStyle()
        {
            return new ThemeStyle(f =>
            {
                if (f is GeometryFeature geometryFeature)
                {
                    return CreateLabelStyle(geometryFeature);
                }
                else
                {
                    return null;
                }
            });
        }
    }
}