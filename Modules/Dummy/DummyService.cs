using Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dummy
{
    public class DummyService : ServiceBase
    {
        public DummyService(string name, ServiceCreationInfo info)
            : base("dummy", info)
        { }

        [ServicePutContract("test/request1")]
        public DateTime OnRequest1()
        {
            return DateTime.Now;
        }

        [ServicePutContract("test/request2")]
        public void OnRequest2()
        {
            throw new ServiceBase.RequestException("Hello exception");
        }

        //[ServicePutContract("test/request3")]
        //public object OnRequest3(string hello, string world, string value = "", string op = "")
        //{
        //    return "request 3: " + hello + ", " + world + ", " + value + ", " + op;
        //}

        [ServicePutContract("test/request4")]
        public void OnRequest4()
        {
            throw new Exception("We're toast!");
        }

        [ServiceGetContract("/{capture}")]
        public dynamic OnRequest5(dynamic parameters)
        {
            return "Foo";
        }
    }
}
