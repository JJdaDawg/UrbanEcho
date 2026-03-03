using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;

namespace UrbanEcho.Models
{
    public class TrafficRule
    {
        private bool blockTraffic;
        private bool isStopSign;
        private bool neverBlock;

        private TrafficRule(bool blockTraffic, bool isStopSign)
        {
            this.blockTraffic = blockTraffic;
            this.isStopSign = isStopSign;
        }

        public static TrafficRule SetDefaultTrafficRule()
        {
            return new TrafficRule(false, false);
        }

        public static TrafficRule SetStopSignTrafficRule()
        {
            return new TrafficRule(true, true);
        }

        public void SetBlock(bool value)
        {
            if (neverBlock && value == true)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Tried to set block traffic on signal that is set to never block"));
            }
            else
            {
                blockTraffic = value;
            }
        }

        public bool IsBlockingTraffic()
        {
            return blockTraffic;
        }

        public bool IsStopSign()
        {
            return isStopSign;
        }

        public void SetNeverBlock()
        {
            neverBlock = true;
        }

        public bool IsNeverBlockingTraffic()
        {
            return neverBlock;
        }
    }
}