using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.UI
{
    public interface IPanelService
    {
        void ToggleConsole(bool open);
        void ToggleRightPanel(bool open);
        void ToggleProperties(bool open);
        void ToggleProjectExplorer(bool open);
    }
}
