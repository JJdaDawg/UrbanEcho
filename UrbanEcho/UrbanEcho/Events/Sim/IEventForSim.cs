using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// The interface for simulation events.
    /// </summary>
    public interface IEventForSim
    {
        public void Run();
    }
}