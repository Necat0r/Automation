using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module
{
    public class ServiceCreationInfo
    {
        public Dictionary<string, string> Configuration;
        public ServiceManager ServiceManager;
        public DeviceManager DeviceManager;

        public ServiceCreationInfo(Dictionary<string, string> configuation, ServiceManager serviceManager, DeviceManager deviceManager)
        {
            this.Configuration = configuation;
            this.ServiceManager = serviceManager;
            this.DeviceManager = deviceManager;
        }
    }
}
