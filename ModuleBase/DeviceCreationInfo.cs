using System.Collections.Generic;

namespace Module
{
    public class DeviceCreationInfo
    {
        public dynamic Configuration;
        public DeviceManager DeviceManager;
        public ServiceManager ServiceManager;

        public DeviceCreationInfo(dynamic configuation, DeviceManager deviceManager, ServiceManager serviceManager)
        {
            this.Configuration = configuation;
            this.DeviceManager = deviceManager;
            this.ServiceManager = serviceManager;
        }
    }
}
