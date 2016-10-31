using Logging;
using Module;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace WakeOnLan
{
    public class WakeOnLanService : ServiceBase
    {
        public WakeOnLanService(ServiceCreationInfo info)
            : base("wake", info)
        { }

        [ServicePutContract()]
        public void OnWakeRequest(string macAddress)
        {
            Log.Info("Attempting to wake up: " + macAddress);

            byte[] buffer = buildPackage(macAddress);
            if (buffer == null)
                throw new ServiceBase.RequestException("Invalid mac address. " + macAddress);

            SendPacket(buffer);
        }

        public void Wake(string macAddress)
        {
            byte[] buffer = buildPackage(macAddress);
            if (buffer == null)
            {
                Log.Warning("Invalid mac address. " + macAddress);
                return;
            }

            SendPacket(buffer);
        }

        private void SendPacket(byte[] packet)
        {
            using (UdpClient client = new UdpClient())
            {
                client.Connect(IPAddress.Broadcast, 7);
                client.Send(packet, packet.Length);
                client.Send(packet, packet.Length);
                client.Send(packet, packet.Length);
                client.Send(packet, packet.Length);
                client.Send(packet, packet.Length);
                client.Send(packet, packet.Length);
            }
        }

        private byte[] parseMacAddress(string macAddress)
        {
            // Handle XX-XX-XX-XX-XX-XX formats
            if (macAddress.Length == 12 + 5)
            {
                string separator = macAddress[2].ToString();
                macAddress = macAddress.Replace(separator, "");
            }

            if (macAddress.Length != 12)
                return null;

            byte[] buffer = new byte[6];
            // Convert to bytes
            for (int i = 0; i < 6; ++i)
            {
                buffer[i] = byte.Parse(macAddress.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return buffer;
        }

        private byte[] buildPackage(string macAddress)
        {
            byte[] macBuffer = parseMacAddress(macAddress);
            if (macBuffer == null)
                return null;

            // Build WOL package.
            byte[] buffer = new byte[17 * 6];

            // First 6 bytes is FF FF FF FF FF FF
            for (int i = 0; i < 6; ++i)
                buffer[i] = 0xFF;

            // 16 repetitions of the mac address
            for (int i = 0; i < 16; ++i)
            {
                var offset = i * 6 + 6;
                macBuffer.CopyTo(buffer, offset);
            }

            return buffer;
        }

    }
}
