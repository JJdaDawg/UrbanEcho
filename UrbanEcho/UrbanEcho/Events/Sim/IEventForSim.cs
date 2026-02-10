using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Events.Sim
{
    internal interface IEventForSim
    {
        public void Run();

        public string Message();
    }
}