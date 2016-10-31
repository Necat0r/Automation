using Microsoft.CSharp.RuntimeBinder;
using Module;
using System;
using System.Collections.Generic;

namespace ModuleBase
{
    public class ServiceFactory
    {
        public static ServiceBase CreateService(ServiceCreationInfo info)
        {
            string typeName;
            try
            {
                typeName = info.Configuration.type;
            }
            catch (RuntimeBinderException)
            {
                throw new ArgumentException("Missing type of service");
            }

            Type serviceType = Type.GetType(typeName);
            if (serviceType == null)
                throw new ArgumentException("Invalid service type name: " + typeName);

            if (!serviceType.IsSubclassOf(typeof(ServiceBase)))
                throw new ArgumentException("Service type is not a subclass of ServiceBase. Type: " + serviceType.Name);

            Object[] arguments = new Object[] { info };

            return (ServiceBase)Activator.CreateInstance(serviceType, arguments);
        }
    }
}
