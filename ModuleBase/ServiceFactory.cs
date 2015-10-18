using Module;
using System;
using System.Collections.Generic;

namespace ModuleBase
{
    public class ServiceFactory
    {
        public static ServiceBase CreateService(string name, string typeName, Dictionary<string, string> settings, ServiceManager serviceManager, DeviceManager deviceManager)
        {
            if (name == null || name.Length == 0)
                throw new ArgumentException("Missing name on service");

            if (typeName == null || typeName.Length == 0)
                throw new ArgumentException("Missing type on service: " + name);

            Type serviceType = Type.GetType(typeName);
            if (serviceType == null)
                throw new ArgumentException("Invalid service type name: " + typeName);

            if (!serviceType.IsSubclassOf(typeof(ServiceBase)))
                throw new ArgumentException("Service type is not a subclass of ServiceBase. Type: " + serviceType.Name);

            ServiceCreationInfo into = new ServiceCreationInfo(settings, serviceManager, deviceManager);
            Object[] arguments = new Object[] { name, into };

            return (ServiceBase)Activator.CreateInstance(serviceType, arguments);
        }
    }
}
