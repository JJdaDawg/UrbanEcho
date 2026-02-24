using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Messages
{
    public class EditModeChangedMessage
    {
        public bool IsEditMode { get; }

        public EditModeChangedMessage(bool isEditMode)
        {
            IsEditMode = isEditMode;
        }
    }
}
