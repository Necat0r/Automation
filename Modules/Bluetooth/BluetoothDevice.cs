using Module;
using System;
using System.Runtime.Serialization;

namespace Bluetooth
{
    public class BluetoothDeviceEvent : EventArgs
    {
        public BluetoothDevice Device;
        public bool InRange;

        public BluetoothDeviceEvent(BluetoothDevice device, bool inRange)
        {
            Device = device;
            InRange = inRange;
        }
    }

    public class BluetoothDevice : DeviceBase
    {
        protected class BluetoothState : DeviceBase.DeviceState
        {
            public BluetoothState()
            {
                InRange = false;
            }

            public BluetoothState(BluetoothState state)
            : base(state)
            {
                BtName = state.BtName;
                InRange = state.InRange;
            }

            public string BtName { get; set; }
            public bool InRange { get; set; }
        }

        public event EventHandler<BluetoothDeviceEvent> OnDeviceEvent;
        private BluetoothService mService = null;

        public BluetoothDevice(DeviceCreationInfo creationInfo)
            : base(new BluetoothState(), creationInfo)
        {
            BluetoothState state = (BluetoothState)mState;
            state.BtName = creationInfo.Configuration.btname;

            mService = (BluetoothService)creationInfo.ServiceManager.GetService(typeof(BluetoothService));
            mService.AddDevice(this);
        }

        ~BluetoothDevice()
        {
            mService.RemoveDevice(this);
        }

        public string BtName
        {
            get
            {
                BluetoothState state = (BluetoothState)mState;
                return state.BtName;
            }
        }

        public bool InRange
        {
            get
            {
                BluetoothState state = (BluetoothState)mState;
                return state.InRange;
            }
        }

        public void OnStatusUpdate(bool inRange, bool initialState = false)
        {
            BluetoothState state = (BluetoothState)mState;
            state.InRange = inRange;

            if (initialState)
                return;

            if (OnDeviceEvent != null)
                OnDeviceEvent(this, new BluetoothDeviceEvent(this, state.InRange));
        }

        public void CheckDevice()
        {
            mService.ForceDeviceUpdate(this);
        }
    }
}
