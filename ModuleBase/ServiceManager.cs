using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module
{
    public class ServiceManager : IDisposable
    {
        private List<ServiceBase> mServices;

        public ServiceManager()
        {
            mServices = new List<ServiceBase>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach(var service in mServices)
                {
                    service.Dispose();
                }
                mServices.Clear();
            }
        }

        public void AddService(ServiceBase service)
        {
            mServices.Add(service);
        }

        public ServiceBase GetService(Type type)
        {
            foreach (var service in mServices)
            {
                Type serviceType = service.GetType();

                if (serviceType.Equals(type) || serviceType.IsSubclassOf(type))
                    return service;
            }

            return null;
        }

        public List<ServiceBase> Services
        {
            get { return mServices; }
        }

        // TODO - Add some sort of key + ability to unregister services so we can update .dll:s in runtime
        // without restarting the application
    }
}
