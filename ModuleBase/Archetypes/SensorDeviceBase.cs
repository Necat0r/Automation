using Module;

namespace ModuleBase.Archetypes
{
    public abstract class SensorDeviceBase : DeviceBase
    {
        protected class SensorState : DeviceBase.DeviceState
        {
            public SensorState()
            {}

            public SensorState(SensorState state)
            : base(state)
            {}

            public new string Archetype { get { return "sensor"; } }
        }

        public SensorDeviceBase(string name, DeviceCreationInfo creationInfo)
        : base(new SensorState(), creationInfo)
        {}

    }
}
