using Logging;
using Module;
using System;
using System.Runtime.InteropServices;

namespace RfxCom
{
    class Lighting2Protocol
    {
        public const byte PacketType = 0x11;

        public static Object BuildPackage(NexaLampDevice device, float level)
        {
            int command = device.Group ? 3 : 0;
            var dimmable = device.Dimmable;

            if (level == 1.0f && !dimmable)
                command += 1;   // Switch it on
            else if (level > 0.0f)
                command += 2;   // Dim it
            // Else turn it off.

            // Convert level to 0-15
            level = level * 15.0f;

            LIGHTING2 package = new LIGHTING2(LIGHTING2.SUBTYPE_AC);
            package.id1 = (byte)((device.Address >> 24) & 0xFF);
            package.id2 = (byte)((device.Address >> 16) & 0xFF);
            package.id3 = (byte)((device.Address >> 8) & 0xFF);
            package.id4 = (byte)(device.Address & 0xFF);
            package.unitcode = (byte)(device.Unit + 1);     // RfxTrx433 uses a 1-based unit
            package.cmnd = (byte)command;
            package.level = (byte)level;

            return package;
        }

        public static NexaEvent HandlePackage(DeviceManager deviceManager, IntPtr memory)
        {
            var package = (LIGHTING2)Marshal.PtrToStructure(memory, typeof(LIGHTING2));

        //    //id1, id2, id3, id4, unit, command, level, rssi = struct.unpack(b'xxxxBBBBBBBB', package)

            int address = package.id1 << 24 | package.id2 << 16 | package.id3 << 8 | package.id4;

            // Make unit start at index zero
            int unit = package.unitcode - 1;

            Log.Debug("Got Nexa event. Address: {0}, unit: {1}, command: {2}, level: {3}, rssi: {4}", address, unit, package.cmnd, package.level, package.rssi);

            //0x00 = off
            //0x01 = on
            //0x02 = set level
            //0x03 = group off
            //0x04 = group on
            //0x05 = group set level
            var isGroupCommand = (package.cmnd >= 0x03);
            var isOnCommand = package.level > 0;

            // TODO - Use a pre-calculated lookup for finding devices

            NexaSensorDevice device = null;
            foreach (var entry in deviceManager.Devices)
            {
                NexaSensorDevice nexaDevice = entry.Value as NexaSensorDevice;
                if (nexaDevice == null)
                    continue;
                
                if (nexaDevice.Address == address && nexaDevice.Unit == unit)
                {
                    device = nexaDevice;
                    break;
                }
            }

            // Convert level to 0-1 range
            float level = (float)package.level;
            level = level / 15.0f;

            return new NexaEvent(device, isOnCommand, level);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
        private struct LIGHTING2
        {
            static public byte LENGTH = 0x0B;
            static public byte TYPE = 0x11;
            static public byte SUBTYPE_AC = 0x00;

            public byte packetlength;
            public byte packettype;
            public byte Subtype;
            public byte seqnbr;
            public byte id1;
            public byte id2;
            public byte id3;
            public byte id4;
            public byte unitcode;
            public byte cmnd;
            public byte level;
            //byte filler2 : 4;
            public byte rssi; // : 4;      // filler (4 bits) + RSSI (4 bits)

            public LIGHTING2(byte subtype)
            {
                packetlength = LENGTH;
                packettype = TYPE;
                Subtype = subtype;
                seqnbr = 0;
                id1 = 0;
                id2 = 0;
                id3 = 0;
                id4 = 0;
                unitcode = 0;
                cmnd = 0;
                level = 0;
                rssi = 0;
            }
        };
    }
}
