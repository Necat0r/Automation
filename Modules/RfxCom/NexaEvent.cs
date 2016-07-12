using System;

namespace RfxCom
{
    public class NexaEvent : EventArgs
    {
        public NexaSensorDevice Device;
        public bool Value;
        public float Level;

        public NexaEvent(NexaSensorDevice device, bool value, float level)
        {
            Device = device;
            Value = value;
            Level = level;
        }
    }
}
