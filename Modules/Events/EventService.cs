using Logging;
using Module;
using System;
using System.Collections.Generic;

namespace Events
{
    public class EventService : ServiceBase
    {
        public class Event : EventArgs
        {
            public string Name { get; set; }
            public Dictionary<string,string> Data { get; set; }
        }

        public event EventHandler<Event> OnEvent;

        public EventService(string name, ServiceCreationInfo info)
            : base("events", info)
        {}

        [ServicePutContract()]
        public void OnEventRequest(Event externalEvent)
        {
            Log.Info("Got event " + externalEvent.Name);
            if (OnEvent != null)
                OnEvent(this, externalEvent);
        }
    }
}
