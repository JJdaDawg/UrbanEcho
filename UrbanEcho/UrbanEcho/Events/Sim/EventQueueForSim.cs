using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    public sealed class EventQueueForSim
    {
        private static EventQueueForSim? instance;
        private ConcurrentQueue<IEventForSim> cq = new ConcurrentQueue<IEventForSim>();

        /// <summary>
        /// Gets Instance of Event Queue For Simulation
        /// </summary>
        /// <returns>EventQueueForSim Instance</returns>
        public static EventQueueForSim Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventQueueForSim();
                }
                return instance;
            }
        }

        /// <summary>
        /// Constructor for EventQueueForSim
        /// </summary>
        /// <returns></returns>
        private EventQueueForSim()
        {
        }
    }
}