using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.Messages
{
    public class LogMessage
    {
        public string Text { get; }
        public LogSource Source { get; }

        public LogMessage(string text, LogSource source)
        {
            Text = text;
            Source = source;
        }
    }
}
