using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.UI
{
    public sealed class EventQueueForUI
    {
        private static EventQueueForUI? instance;
        private ConcurrentQueue<IEventForUI> cq = new ConcurrentQueue<IEventForUI>();

        /// <summary>
        /// Gets Instance of Event Queue For UI
        /// </summary>
        /// <returns>EventQueueForSim Instance</returns>
        public static EventQueueForUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventQueueForUI();
                }
                return instance;
            }
        }

        /// <summary>
        /// Constructor for EventQueueForUI
        /// </summary>
        /// <returns></returns>
        private EventQueueForUI()
        {
        }
    }
}