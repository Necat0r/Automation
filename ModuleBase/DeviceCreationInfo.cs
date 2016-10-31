using System.Collections.Generic;

namespace Module
{
    public class DeviceCreationInfo
    {
        public dynamic Configuration;
        public DeviceManager DeviceManager;
        public ServiceManager ServiceManager;

        public DeviceCreationInfo(dynamic configuation, ServiceManager serviceManager, DeviceManager deviceManager)
        {
            this.Configuration = configuation;
            this.ServiceManager = serviceManager;
            this.DeviceManager = deviceManager;
        }
    }
}
