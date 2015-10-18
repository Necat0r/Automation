using System.Collections.Generic;

namespace Module
{
    public class ProxyDevice : DeviceBase
    {
        public ProxyDevice(DeviceBase.DeviceState state, DeviceCreationInfo creationInfo)
        : base(state, creationInfo)
        {}

        protected void RunRequest(List<string> parameters, Dictionary<string, string> arguments)
        {
            /*
            //if (not arguments)
            //    arguments = {};
            string[] paramsArray = parameters.ToArray();
            string uri = "/" + string.Join("/", parameters.ToArray());

            if (arguments.Count > 0)
            {
                from arg in arguments select value1 + "=" + value2;


                uri += "?" + string.Join("&", [str(x[0]) + "=" + str(x[1]) for x in arguments.items()])
            }

            try
            {
                conn = HTTPConnection(self.ipAddress, self.port, timeout=2)
                conn.connect()
                request = conn.putrequest("GET", uri)
                conn.endheaders()
            
                resp = conn.getresponse()
                print(resp.status)
                print(resp.reason)
                print(resp.read())
                conn.close()
            }
            catch (Exception e)
            {
            //except socket.timeout as timeout:
            //    print('Request timed out for:', uri)
            //except Exception as e:
            //    print('ERROR: ' + e)
            }
            */
        }
    }
}
