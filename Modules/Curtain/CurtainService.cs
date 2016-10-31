using Logging;
using Module;
using Support;
using System;
using System.Threading.Tasks;

namespace Curtain
{
    public class CurtainService : ServiceBase, IDisposable
    {
        private SerialHelper mSerialHelper;
        private RadioLock mRadioLock;
        private Object mLock;

        private const int TransmissionDuration = 200;

        public CurtainService(ServiceCreationInfo info)
            : base("curtain", info)
        {
            uint port = uint.Parse(info.Configuration.port);

            mSerialHelper = SerialRepository.OpenPort("arduino", port, 115200);
            //mSerialHelper = new SerialHelper("curtain", (uint)port, 115200);
            mRadioLock = RadioLock.Instance;

            mLock = new Object();
        }

        public void Dispose()
        {
            mSerialHelper.Dispose();
        }

        async public void SendData(byte[] data)
        {
            // Early out in case we're not connected yet.
            if (!mSerialHelper.IsConnected)
            {
                Log.Warning("Discarding curtain package since we're not connected");
            }

            // Acquire the lock for a bit longer than what we need so it won't timeout.
            using (await mRadioLock.AquireAsync(TransmissionDuration + 50, "curtain"))
            {
                lock (mLock)
                {
                    // Send data.
                    if (mSerialHelper.IsConnected)
                        mSerialHelper.WriteData(data);
                }

                // Delay for the transmission duration so we don't release the lock too early.
                await Task.Delay(TransmissionDuration);
            }
        }
    }
}
