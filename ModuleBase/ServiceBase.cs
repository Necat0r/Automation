using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Linq;

namespace Module
{
    public class ServiceBase : IDisposable
    {
        [Serializable]
        public class RequestException : Exception
        {
            public RequestException(string message)
                : base(message)
            { }
        }

        [Serializable]
        public class ServiceException : Exception
        {
            public ServiceException(string message)
                : base(message)
            { }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public class ServiceGetContractAttribute : Attribute
        {
            public readonly string uri;

            public ServiceGetContractAttribute() { }

            public ServiceGetContractAttribute(string uri)
            {
                // Validate contract format
                if (uri.Count(c => c == '{') != uri.Count(c => c == '}'))
                    throw new ArgumentException("Contract URI have misaligned curly brackets. Uri: " + uri);

                this.uri = uri;
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class ServicePutContractAttribute : ServiceGetContractAttribute
        {
            public ServicePutContractAttribute() { }

            public ServicePutContractAttribute(string uri)
                : base(uri)
            { }
        }

        private string mName;

        public ServiceBase(string name, ServiceCreationInfo info)
        {
            if (name == null || name.Length == 0)
                throw new ArgumentException("Missing name on service");

            mName = name;
        }


        public ServiceBase(ServiceCreationInfo info)
        {
            string name;
            try
            {
                name = info.Configuration.Name;
            }
            catch (RuntimeBinderException)
            {
                throw new ArgumentException("Missing name of service");
            }

            if (name.Length == 0)
                throw new ArgumentException("Missing name on service");

            mName = name;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {}

        public string Name
        {
            get { return mName; }
        }

    }
}
