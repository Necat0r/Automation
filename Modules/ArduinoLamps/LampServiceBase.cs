using Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoLamps
{
    public abstract class LampServiceBase : ServiceBase
    {
        public LampServiceBase(string name, ServiceCreationInfo info)
            : base(name, info)
        {
        }

        public abstract void SetLevel(LampDevice lampDevice, float level);
    }
}
