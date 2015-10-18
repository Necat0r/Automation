using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluetooth
{
    public class BluetoothEvent
    {
        public string Name;
        public bool InRange;

        public BluetoothEvent(string name, bool inRange)
        {
            Name = name;
            InRange = inRange;
        }
    }
}
