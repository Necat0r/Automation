using Module;
using ModuleBase;
using System;
using System.Collections.Generic;

namespace ModuleBaseTest
{
    public class TestUtil
    {
        public DeviceManager DeviceManager { get; set; }
        public ServiceManager ServiceManager { get; set; }

        public TestUtil()
        {
            DeviceManager = new DeviceManager();
            ServiceManager = new ServiceManager();
        }

        public DeviceBase CreateDevice(dynamic deviceConfiguration)
        {
            var info = new DeviceCreationInfo(deviceConfiguration, ServiceManager, DeviceManager);
            var device = DeviceFactory.CreateDevice(info);

            DeviceManager.AddDevice(device);

            return device;
        }

        public ServiceBase CreateService(dynamic configuration)
        {
            var info = new ServiceCreationInfo(configuration, ServiceManager, DeviceManager);
            var service = ServiceFactory.CreateService(info);

            ServiceManager.AddService(service);

            return service;
        }

        public static dynamic GetDefaultDeviceConfig(Type deviceType)
        {
            dynamic config = new SettingsObject();
            config.name = "ProperDeviceName";
            config.voiceName = "ProperDeviceVoiceName";
            config.type = deviceType.AssemblyQualifiedName;

            return config;
        }
    }
}
