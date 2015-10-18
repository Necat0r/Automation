using Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleBase.Archetypes
{
    public abstract class CurtainDeviceBase : DeviceBase
    {
        protected class CurtainState : DeviceBase.DeviceState
        {
            public CurtainState()
            { }

            public CurtainState(CurtainState state)
            : base(state)
            {}

            public new string Archetype { get { return "curtain"; } }
        }

        protected CurtainDeviceBase(CurtainState state, DeviceCreationInfo creationInfo)
        : base(state, creationInfo)
        {}

        public abstract void Up();
        public abstract void Stop();
        public abstract void Down();

        public abstract float Position
        {
            get;
        }
    }
}
