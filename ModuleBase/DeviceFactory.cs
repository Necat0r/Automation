using Microsoft.CSharp.RuntimeBinder;
using Module;
using System;

namespace ModuleBase
{
    public class DeviceFactory
    {
        public static DeviceBase CreateDevice(DeviceCreationInfo info)
        {
            string type;
            try
            {
                type = info.Configuration.type;
            }
            catch (RuntimeBinderException)
            {
                throw new ArgumentException("Missing type of device");
            }

            Type deviceType = Type.GetType(type);
            if (deviceType == null)
                throw new ArgumentException("Invalid device type name " + type);

            if (!deviceType.IsSubclassOf(typeof(DeviceBase)))
                throw new ArgumentException("Type is not a subclass of DeviceBase. Type: " + deviceType.Name);

            Object[] arguments = new Object[] { info };

            return (DeviceBase)Activator.CreateInstance(deviceType, arguments);
        }
    }
}
