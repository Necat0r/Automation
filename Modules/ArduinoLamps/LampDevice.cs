using Module;
using ModuleBase.Archetypes;
using System;

namespace ArduinoLamps
{
    public class LampDevice : LampDeviceBase
    {
        public new class LampState : LampDeviceBase.LampState
        {
            public LampState()
            {
                Channel = 0;
            }

            public LampState(LampState state)
                : base(state)
            {
                Channel = state.Channel;
            }

            public int Channel { get; set; }
        }

        public int Channel { get { return State.Channel; } }

        private LampServiceBase mService;

        private LampState State { get { return (LampState)mState; }}

        public LampDevice(DeviceCreationInfo creationInfo)
            : base(new LampState(), creationInfo)
        {
            mService = (LampServiceBase)creationInfo.ServiceManager.GetService(typeof(LampServiceBase));
            if (mService == null)
                throw new InvalidOperationException("LampService is missing, device cannot run");

            State.Dimmable = true;
            State.Channel = int.Parse(creationInfo.Configuration.channel);
        }

        protected override bool OnSwitchDevice(bool value)
        {
            float level = value ? 1.0f : 0.0f;

            mService.SetLevel(this, level);
            return true;
        }

        protected override bool OnDimDevice(float level)
        {
            level = Math.Max(0.0f, Math.Min(level, 1.0f));
            mService.SetLevel(this, level);
            return true;
        }
    }
}
