using Logging;
using System;
using System.Collections.Generic;

namespace Support
{
    public class SerialRepository
    {
        private static Dictionary<uint, SerialHelper> mPortLookup = new Dictionary<uint, SerialHelper>();

        public static SerialHelper OpenPort(string name, uint port, uint baudrate)
        {
            SerialHelper helper;

            lock (mPortLookup)
            {
                bool exists = mPortLookup.TryGetValue(port, out helper);

                if (!exists)
                {
                    Log.Info("Opening COM{0} with handle '{1}' at {2} baud", port, name, baudrate);
                    helper = new SerialHelper(name, port, baudrate);
                    mPortLookup.Add(port, helper);
                }
                else
                {
                    if (helper.Baudrate != baudrate)
                        throw new ArgumentException(string.Format("Attempt to open COM{0} in {1} baud when it was previously opened in {2} baud. Original handle: {3}.", port, baudrate, helper.Baudrate, helper.Name));

                    Log.Info("Reusing COM{0} at {1} baud via handle '{2}'", port, baudrate, name);
                }
            }

            return helper;
        }
    }
}
