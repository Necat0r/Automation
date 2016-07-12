using Module;
using System;
using System.Runtime.InteropServices;

namespace RfxCom
{
    class Lighting5Protocol
    {
        public const byte PacketType = 0x14;

        public static Object BuildPackage(EverflourishDevice device, bool value)
        {
            Console.WriteLine("Sending lighting 5 package");

            LIGHTING5 package = new LIGHTING5();
            package.id1 = 0;
            package.id2 = (byte)((device.Address >> 8) & 0x3F);
            package.id3 = (byte)(device.Address & 0xFF);
            package.unitcode = (byte)device.Unit;
            package.cmnd = (byte)(value ? 1 : 0);
            package.level = (byte)(value ? 0xFF : 0x00);

            return package;
        }

        public static EverflourishEvent HandlePackage(DeviceManager deviceManager, IntPtr memory)
        {
            return null;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LIGHTING5
        {
            static byte LENGTH = 0x0A;
            static byte TYPE = 0x14;
            static byte SUBTYPE_EMW100 = 0x01;

            public byte packetlength;
            public byte packettype;
            public byte subtype;
            public byte seqnbr;
            public byte id1;
            public byte id2;
            public byte id3;
            public byte unitcode;
            public byte cmnd;
            public byte level;
            //byte filler : 4;
            public byte rssi; // : 4;

            public LIGHTING5(bool dummy = true)
            {
                packetlength = LENGTH;
                packettype = TYPE;
                subtype = SUBTYPE_EMW100;
                seqnbr = 0;
                id1 = 0;
                id2 = 0;
                id3 = 0;
                unitcode = 0;
                cmnd = 0;
                level = 0;
                rssi = 0;
            }
        };
    }
}
