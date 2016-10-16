using Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModuleBaseTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModulesTest
{
    [TestClass]
    public class EventTest
    {
        private EventService CreateEventService()
        {
            var util = new TestUtil();
            string name = "EventService";
            string type = typeof(EventService).AssemblyQualifiedName;

            var serviceConfiguration = new Dictionary<string, string>();

            return (EventService) util.CreateService(name, type, serviceConfiguration);
        }

        private EventService.Event CreateTestEvent()
        {
            var testEvent = new EventService.Event();
            testEvent.Name = "testEvent";
            testEvent.Data.Add("hello", "world");

            return testEvent;
        }

        private bool AreEqual<K, V>(Dictionary<K, V> lhs, Dictionary<K, V> rhs)
        {
            return lhs.Count == rhs.Count &&
                lhs.Keys.All(k => rhs.ContainsKey(k) && object.Equals(lhs[k], rhs[k]));
        }
            

        [TestMethod]
        public void EventService_TestCreation()
        {
            EventService service = CreateEventService();
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void EventService_TestNull()
        {
            var service = CreateEventService();

            service.OnEvent += (sender, serviceEvent) =>
                {
                    Assert.Fail();
                };

            service.OnEventRequest(null);
        }

        [TestMethod]
        public void EventService_TestEvent()
        {
            var service = CreateEventService();

            var testEvent = CreateTestEvent();
            var externalEvent = CreateTestEvent();

            bool eventTriggered = false;

            service.OnEvent += (sender, serviceEvent) =>
                {
                    Assert.AreEqual(testEvent.Name, serviceEvent.Name);
                    Assert.IsTrue(AreEqual(testEvent.Data, serviceEvent.Data));

                    eventTriggered = true;
                };

            service.OnEventRequest(externalEvent);

            Assert.IsTrue(eventTriggered);
        }

        [TestMethod]
        public void EventService_TestMultipleListeners()
        {
            var service = CreateEventService();
            var testEvent = CreateTestEvent();

            int eventCount = 0;
            
            EventHandler<EventService.Event> handler = (sender, serviceEvent) =>
                {
                    ++eventCount;
                };

            service.OnEvent += handler;
            service.OnEvent += handler;
            service.OnEvent += handler;

            service.OnEventRequest(testEvent);

            Assert.AreEqual(3, eventCount);
        }
    }
}
