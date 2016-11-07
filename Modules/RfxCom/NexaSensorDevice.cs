using Module;
using ModuleBase.Archetypes;
using System;

namespace RfxCom
{
    public class NexaSensorDevice : SensorDeviceBase
    {
        protected class NexaSensorState : SensorState
        {
            public NexaSensorState()
            {
                Address = 0;
                Unit = 0;
            }

            public NexaSensorState(NexaSensorState state)
                : base(state)
            {
                Address = state.Address;
                Unit = state.Unit;
            }

            public int Address { get; set; }
            public int Unit { get; set; }
        }

        public event EventHandler<NexaEvent> OnDeviceEvent = delegate { };

        public NexaSensorDevice(DeviceCreationInfo creationInfo)
        : base(new NexaSensorState(), creationInfo)
        {
            ((NexaSensorState)mState).Address = int.Parse(creationInfo.Configuration.code);
            ((NexaSensorState)mState).Unit = int.Parse(creationInfo.Configuration.unit);
        }

        // Called from RfxComService
        public void OnServiceEvent(NexaEvent nexaEvent)
        {
            OnDeviceEvent(this, nexaEvent);
        }

        public int Address { get { return ((NexaSensorState)mState).Address; } }
        public int Unit { get { return ((NexaSensorState)mState).Unit; } }
    }
}
