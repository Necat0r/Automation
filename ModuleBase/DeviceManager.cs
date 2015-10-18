using Logging;
using System.Collections.Generic;

namespace Module
{
    public class DeviceManager
    {
        private Dictionary<string, DeviceBase> mDevices = new Dictionary<string, DeviceBase>();

        public void AddDevice(DeviceBase device)
        {
            mDevices.Add(device.Name, device);
            //Log.Info("Adding device. Name: {0}, Type: {1}", device.Name, device.GetType().Name);
        }

        public DeviceBase GetDevice(string deviceName)
        {
            DeviceBase device;
            
            if (mDevices.TryGetValue(deviceName, out device))
                return device;

            return null;
        }

        public Dictionary<string, DeviceBase> Devices
        {
            get { return mDevices; }
        }
    }
}
