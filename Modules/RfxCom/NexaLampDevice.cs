using Microsoft.CSharp.RuntimeBinder;
using Module;
using ModuleBase.Archetypes;
using System;
using System.Diagnostics;

namespace RfxCom
{
    public class NexaLampDevice : LampDeviceBase
    {
        protected class NexaLampState : LampState
        {
            public NexaLampState()
            {
                Address = 0;
                Unit = 0;
            }

            public NexaLampState(NexaLampState state)
            : base(state)
            {
                Address = state.Address;
                Unit = state.Unit;
            }

            public int Address { get; set; }
            public int Unit { get; set; }
        }

        private RfxComService mService;

        public NexaLampDevice(DeviceCreationInfo creationInfo)
        : base(new NexaLampState(), creationInfo)
        {
            // Enable forced switching since we don't have a closed loop and prediction is causing state changes to be
            // disregarded since LampDeviceBase thinks it's already in that state.
            ForceSwitching = true;

            mService = (RfxComService)creationInfo.ServiceManager.GetService(typeof(RfxComService));
            if (mService == null)
                throw new InvalidOperationException("RfxComService is missing, device cannot run");

            ((NexaLampState)mState).Address = int.Parse(creationInfo.Configuration.code);
            ((NexaLampState)mState).Unit = int.Parse(creationInfo.Configuration.unit);

            try
            {
                ((NexaLampState)mState).Group = bool.Parse(creationInfo.Configuration.group);
            }
            catch (RuntimeBinderException) { }

            try
            {
                ((NexaLampState)mState).Dimmable = bool.Parse(creationInfo.Configuration.dimmable);
            }
            catch (RuntimeBinderException) { }
        }

        public override void ApplyState(DeviceBase.DeviceState state)
        {
            base.ApplyState(state);
           
            NexaLampState currentState = (NexaLampState)mState;
            NexaLampState newState = (NexaLampState)state;

            Debug.Assert(newState.Address == currentState.Address);
            Debug.Assert(newState.Unit == currentState.Unit);
        }

        protected override bool OnSwitchDevice(bool value)
        {
            return mService.SwitchDevice(this, value);
        }

        protected override bool OnDimDevice(float level)
        {
            return mService.DimDevice(this, level);
        }

        public int Address { get { return ((NexaLampState)mState).Address; } }
        public int Unit { get { return ((NexaLampState)mState).Unit; } }
    }
}
