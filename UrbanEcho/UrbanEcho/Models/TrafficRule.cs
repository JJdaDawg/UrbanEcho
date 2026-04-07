using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;

namespace UrbanEcho.Models
{
    /// <summary>
    /// This class provides Traffic rule. indicating if it
    /// is blocking traffic
    /// is never blocking traffic
    /// is a stop sign
    /// </summary>
    public class TrafficRule
    {
        private bool blockTraffic;
        private bool isStopSign;
        private bool neverBlock;

        private TrafficRule(bool blockTraffic, bool isStopSign, bool neverBlock = false)
        {
            this.blockTraffic = blockTraffic;
            this.isStopSign = isStopSign;
            this.neverBlock = neverBlock;
        }

        /// <summary>
        /// Sets up a default traffic rule
        /// </summary>
        public static TrafficRule SetDefaultTrafficRule()
        {
            return new TrafficRule(false, false);
        }

        /// <summary>
        /// Sets up a fall back traffic rule
        /// </summary>
        public static TrafficRule SetFallBackTrafficRule()
        {
            return new TrafficRule(false, false, true);
        }

        /// <summary>
        /// Sets up a stop sign traffic rule
        /// </summary>
        public static TrafficRule SetStopSignTrafficRule()
        {
            return new TrafficRule(true, true);
        }

        /// <summary>
        /// Sets if this traffic rule should be blocking traffic
        /// </summary>
        public void SetBlock(bool value)
        {
            if (neverBlock && value == true)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Tried to set block traffic on signal that is set to never block"));
            }
            else
            {
                blockTraffic = value;
            }
        }

        /// <summary>
        /// Gets if this traffic rule should be blocking traffic
        /// </summary>
        public bool IsBlockingTraffic()
        {
            return blockTraffic;
        }

        /// <summary>
        /// Gets if this traffic rule is a stop sign
        /// </summary>
        public bool IsStopSign()
        {
            return isStopSign;
        }

        /// <summary>
        /// Sets if this traffic rule should be never be blocking traffic
        /// </summary>
        public void SetNeverBlock()
        {
            neverBlock = true;
        }

        /// <summary>
        /// Gets if this traffic rule should be never blocking traffic
        /// </summary>
        public bool IsNeverBlockingTraffic()
        {
            return neverBlock;
        }
    }
}