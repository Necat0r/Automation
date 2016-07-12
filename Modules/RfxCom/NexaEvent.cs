using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RfxCom
{
    public class NexaEvent : EventArgs
    {
        public NexaDevice Device;
        public bool Value;
        public float Level;

        public NexaEvent(NexaDevice device, bool value, float level)
        {
            Device = device;
            Value = value;
            Level = level;
        }
    }
}
