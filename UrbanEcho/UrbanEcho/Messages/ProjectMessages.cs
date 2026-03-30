using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Messages
{
    public class ProjectLoadedMessage { }
    public class ProjectClosedMessage { }

    public class AadtReadyMessage
    {
        public bool HasRealAadt { get; }
        public AadtReadyMessage(bool hasRealAadt) => HasRealAadt = hasRealAadt;
    }
}
