using Module;
using Support;

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
            byte byteLevel = (byte)(level * 255.0f);

            // size, payload[size]
            byte[] data = new byte[] { (byte)3, (byte)DEVICE_ID, (byte)lampDevice.Channel, (byte)byteLevel };

            if (mSerialHelper.IsConnected)
                mSerialHelper.WriteData(data);
        }
    }
}