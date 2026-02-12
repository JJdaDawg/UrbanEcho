using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;

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

        /// <summary>
        /// Returns status of the queue
        /// </summary>
        /// <returns>Returns true if queue is empty</returns>
        public bool IsEmpty()
        {
            return cq.IsEmpty;
        }

        /// <summary>
        /// Adds a Event to the queue for the UI to read
        /// </summary>
        /// <param name="theEvent">The Event to add.</param>
        /// <returns></returns>
        public void Add(IEventForUI theEvent)
        {
            cq.Enqueue(theEvent);
        }

        /// <summary>
        /// UI Reads a Event from the queue
        /// </summary>
        /// <returns>Returns the event at beginning of queue or nothing if empty</returns>
        public IEventForUI? Read()
        {
            IEventForUI? itemInQueue;
            cq.TryDequeue(out itemInQueue);

            return itemInQueue;
        }
    }
}