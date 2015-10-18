using Module;

namespace Dummy
{
    public class DummyDevice : DeviceBase
    {
        protected class DummyState : DeviceBase.DeviceState
        {
            public DummyState()
            {

            }

            public DummyState(DummyState state)
                : base(state)
            {
            }

            public new string Archetype { get { return "dummy"; } }
        }


        public DummyDevice(DeviceCreationInfo creationInfo)
            : base(new DummyState(), creationInfo)
        {}

    }
}
