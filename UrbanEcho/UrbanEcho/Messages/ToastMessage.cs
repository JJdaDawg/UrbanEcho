using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Messages
{
    public class ShowToastMessage
    {
        public string Text { get; }
        public ShowToastMessage(string text) => Text = text;
    }

    public class HideToastMessage { }
}
