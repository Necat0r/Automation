using Module;
using System;
using System.Collections.Generic;

namespace Events
{
    public class EventService : ServiceBase
    {
        public class Event
        {
            public string Name { get; set; }
            public object Data { get; set; }
        }

        public EventService(string name, ServiceCreationInfo info)
            : base("events", info)
        {
        }

        //[ServicePutContract()]
        //public void OnEventRequest(dynamic parameters, dynamic body)
        //{
        //    Event requestEvent = DynamicMapper.Map<Event>(body);

        //    Console.WriteLine("Got event with name: " + requestEvent.Name + ", body name: " + body.Name);
        //}

        [ServicePutContract()]
        public void OnEventRequest(Event externalEvent)
        {

        }
    }
}
