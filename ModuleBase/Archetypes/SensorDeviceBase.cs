using Module;

namespace ModuleBase.Archetypes
{
    public abstract class SensorDeviceBase : DeviceBase
    {
        public class SensorState : DeviceBase.DeviceState
        {
            public SensorState()
            {}

            public SensorState(SensorState state)
            : base(state)
            {}

            public new string Archetype { get { return "sensor"; } }
        }

        public SensorDeviceBase(SensorState state, DeviceCreationInfo creationInfo)
        : base(state, creationInfo)
        {}
    }
}
