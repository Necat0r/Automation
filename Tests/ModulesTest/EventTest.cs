using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModuleBaseTest;
using Events;

namespace ModulesTest
{
    [TestClass]
    public class EventTest
    {
        [TestMethod]
        public void EventService_TestCreation()
        {
            var util = new TestUtil();

            string name = "LampTest";
            string type = typeof(EventService).AssemblyQualifiedName;

            var serviceConfiguration = new Dictionary<string, string>();
            util.CreateService(name, type, serviceConfiguration);
        }
    }
}
