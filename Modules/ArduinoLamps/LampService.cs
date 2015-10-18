using Module;
using Support;
using System;
using System.Diagnostics;

namespace ArduinoLamps
{
    public class LampService : LampServiceBase
    {
        private readonly int DEVICE_ID = 3;

        private SerialHelper mSerialHelper;

        public LampService(string name, ServiceCreationInfo info)
            : base(name, info)
        {
            uint port = uint.Parse(info.Configuration["port"]);

            mSerialHelper = SerialRepository.OpenPort("arduino", port, 115200);
        }

        public override void SetLevel(LampDevice lampDevice, float level)
        {
            // level should already be clamped
            Debug.Assert(level >= 0.0f && level <= 1.0f, "Level should already have been clamped!");

            byte byteLevel = (byte)(level * 255.0f);

            // size, payload[size]
            byte[] data = new byte[] { (byte)3, (byte)DEVICE_ID, (byte)lampDevice.Channel, (byte)byteLevel };

            if (mSerialHelper.IsConnected)
                mSerialHelper.WriteData(data);
        }
    }
}