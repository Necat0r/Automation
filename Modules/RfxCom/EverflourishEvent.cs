using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RfxComService
{
    public class EverflourishEvent : EventArgs
    {
        public EverflourishDevice Device;
        public bool Value;

        public EverflourishEvent(EverflourishDevice device, bool value)
        {
            Device = device;
            Value = value;
        }
    }
}
