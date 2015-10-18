using Microsoft.CSharp.RuntimeBinder;
using Module;
using ModuleBase.Archetypes;

namespace RfxComService
{
    public class EverflourishDevice : LampDeviceBase
    {
        protected class EverflourishState : LampState
        {
            public EverflourishState()
            {
                Address = 0;
                Unit = 0;
            }

            public EverflourishState(EverflourishState state)
            : base(state)
            {
                Address = state.Address;
                Unit = state.Unit;
            }

            public int Address { get; set; }
            public int Unit { get; set; }
        }

        private RfxComService mService;

        public EverflourishDevice(DeviceCreationInfo creationInfo)
        : base(new EverflourishState(), creationInfo)
        {
            mService = (RfxComService)creationInfo.ServiceManager.GetService(typeof(RfxComService));
            ((EverflourishState)mState).Address = int.Parse(creationInfo.Configuration.code);
            ((EverflourishState)mState).Unit = int.Parse(creationInfo.Configuration.unit);

            try
            {
                ((EverflourishState)mState).Group = bool.Parse(creationInfo.Configuration.group);
            }
            catch (RuntimeBinderException) { }
        }

        protected override bool OnSwitchDevice(bool value)
        {
            return mService.SwitchDevice(this, value);
        }

        protected override bool OnDimDevice(float level)
        {
            return false;
        }

        public int Address { get { return ((EverflourishState)mState).Address; } }
        public int Unit { get { return ((EverflourishState)mState).Unit; } }
        public bool IsGroup { get { return ((EverflourishState)mState).Group; } }
    }
}
