using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module
{
    public class ServiceBase
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
            mName = name;
        }

        public string Name
        {
            get { return mName; }
        }
    }
}
