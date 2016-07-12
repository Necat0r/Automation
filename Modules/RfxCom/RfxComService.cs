using Module;
using Support;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace RfxCom
{
    public class RfxComService : ServiceBase, SerialHelper.SerialListener, IDisposable
    {
        private SerialHelper mSerialHelper;
        private RadioLock mRadioLock;
        private bool mLockOwned;

        bool mRunning = true;
        private Thread mRfxThread;
        private AutoResetEvent mActionEvent = new AutoResetEvent(false);
        private AutoResetEvent mSerialEvent = new AutoResetEvent(false);
        private ManualResetEvent mStopEvent = new ManualResetEvent(false);
        private EventWaitHandle[] mEvents;

        private TimeSpan mPackageTimeout = new TimeSpan(0, 0, 10);
        private DateTime mLastReceive;

        private SerialHelper.Buffer mBuffer = new SerialHelper.Buffer();

        protected struct DeviceAction
        {
            public DeviceBase Device;
            public bool Value;
            public float Level;

            public DeviceAction(DeviceBase device, bool value)
            {
                Device = device;
                Value = value;
                Level = value ? 1.0f : 0.0f;
            }

            public DeviceAction(DeviceBase device, float level)
            {
                Device = device;
                Level = level;
                Value = level > 0.0f;
            }
        }

        protected Queue<DeviceAction> mActionQueue = new Queue<DeviceAction>();

        private DeviceManager mDeviceManager;

        public event EventHandler<EverflourishEvent> OnEverflourishEvent;

        public RfxComService(string name, ServiceCreationInfo info)
            : base ("rfxcom", info)
        {
            mDeviceManager = info.DeviceManager;

            int port = int.Parse(info.Configuration["port"]);

            mSerialHelper = new SerialHelper("rfxcom", (uint)port, 38400);
            mRadioLock = RadioLock.Instance;
            mEvents = new EventWaitHandle[] { mActionEvent, mSerialEvent, mStopEvent };

            mRfxThread = new Thread(Tick);
            mRfxThread.Start();
        }

        public void Dispose()
        {
            mRunning = false;
            mStopEvent.Set();
            mRfxThread.Join();

            mSerialHelper.Dispose();
            mActionEvent.Dispose();
            mSerialEvent.Dispose();
            mStopEvent.Dispose();
        }

        public bool SwitchDevice(DeviceBase device, bool value)
        {
            if (!mSerialHelper.IsConnected)
            {
                Console.WriteLine("Disregarding SwitchDevice() request for device {0} since we're not connected", device.Name);
                return false;
            }

            lock (mActionQueue)
            {
                // Double send to try avoid failed sends
                mActionQueue.Enqueue(new DeviceAction(device, value));
                mActionQueue.Enqueue(new DeviceAction(device, value));
            }

            // Notify thread that there's stuff to do
            mActionEvent.Set();

            return true;
        }

        public bool DimDevice(DeviceBase device, float level)
        {
            if (!mSerialHelper.IsConnected)
            {
                Console.WriteLine("Disregarding DimDevice() request for device {0} since we're not connected", device.Name);
                return false;
            }

            level = Math.Min(Math.Max(level, 0.0f), 1.0f);

            lock (mActionQueue)
            {
                // Double send to try avoid failed sends
                mActionQueue.Enqueue(new DeviceAction(device, level));
                mActionQueue.Enqueue(new DeviceAction(device, level));
            }

            // Notify thread that there's stuff to do
            mActionEvent.Set();

            return true;
        }

        public void OnConnected()
        {
            mBuffer.Clear();
        }

        public void OnDisconnected()
        {
            // We've just been disconnected, purge the action queue
            lock (mActionQueue)
            {
                mActionQueue.Clear();
            }

            // Purge package buffer
            lock (mBuffer)
            {
                mBuffer.Clear();
            }
        }

        public void OnData(byte[] data)
        {
            var currTime = DateTime.Now;

            lock (mBuffer)
            {
                // Purge stale data
                if (mLastReceive != null && !mBuffer.IsEmpty && (mPackageTimeout < currTime - mLastReceive))
                {
                    Console.WriteLine("Discarding stale data");
                    mBuffer.Clear();
                }

                mLastReceive = currTime;
                mBuffer.Write(data);
            }

            // Notify local thread that there's data available.
            mSerialEvent.Set();
        }

        public void Tick()
        {
            mSerialHelper.AddListener(this);

            while (mRunning)
            {
                try
                {
                    // Reset these first so we don't have the other thread set them after we've checked the data and end up
                    // sleeping through it
                    mSerialEvent.Reset();
                    mActionEvent.Reset();

                    if (mActionQueue.Count == 0 && mBuffer.IsEmpty)
                    {
                        // Sleep until something happens.
                        EventWaitHandle.WaitAny(mEvents);
                    }

                    bool waiting = false;
                    if (mActionQueue.Count > 0)
                        ProcessActions(out waiting);

                    if (!mBuffer.IsEmpty)
                        ProcessData();

                    if (waiting)
                    {
                        // Stay a while and listen.
                        EventWaitHandle.WaitAny(mEvents, 100);
                    }
                }
                catch (Exception e)
                {
                    Logging.Log.Exception(e);
                }
            }

            mSerialHelper.RemoveListener(this);
        }

        private void ProcessActions(out bool waiting)
        {
            waiting = false;

            // Wait until there's actions to be processed.
            if (mActionQueue.Count == 0)
            {
                return;
            }

            // Acquire radio lock
            if (!mRadioLock.TryEnter(2000, "rfxcom"))
            {
                //// Wait a while for the radio lock to be freed up.
                //EventWaitHandle.WaitAny(mEvents, 100);
                waiting = true;
                return;
            }

            lock (mRadioLock)
            {
                mLockOwned = true;
            }

            DeviceAction action;
            lock (mActionQueue)
            {
                action = mActionQueue.Dequeue();
            }

            bool result = false;
            DeviceBase device = action.Device;
            if (device is NexaLampDevice)
            {
                var package = Lighting2Protocol.BuildPackage((NexaLampDevice)device, action.Level);
                result = SendPackage(package);
            }
            else if (device is EverflourishDevice)
            {
                var package = Lighting5Protocol.BuildPackage((EverflourishDevice)device, action.Value);
                result = SendPackage(package);
            }

            if (!result)
            {
                Console.WriteLine("Failed processing action, releasing lock directly");
                mRadioLock.Release();
            }
        }

        private void ProcessData()
        {
            byte[] packetData = null;
            lock (mBuffer)
            {
                var packageLength = mBuffer.Data[0];
                if (packageLength < Marshal.SizeOf(typeof(RFXPACKET)))
                {
                    Console.WriteLine("Package too small. Disconnecting to retry again");
                    mSerialHelper.Disconnect();
                    return;
                }
                else if (mBuffer.Data.Length >= packageLength + 1)
                {
                    packetData = mBuffer.Consume(packageLength + 1);
                }
            }

            // Process the package
            if (packetData != null)
                HandlePackage(packetData);
        }

        private void HandlePackage(byte[] data)
        {
            var packageSize = data.Length;
            IntPtr memory = Marshal.AllocHGlobal(packageSize);

            // Decode first part of the package so we can detect the type
            Marshal.Copy(data, 0, memory, packageSize);
            RFXPACKET packet = (RFXPACKET)Marshal.PtrToStructure(memory, typeof(RFXPACKET));

            if (packet.PacketType == IRESPONSE.TYPE)
            {
                // TODO - Log errors
            }
            else if (packet.PacketType == RXRESPONSE.TYPE)
            {
                HandleRXResponsePackage(memory);
            }
            else if (packet.PacketType == Lighting2Protocol.PacketType)
            {
                NexaEvent nexaEvent = Lighting2Protocol.HandlePackage(mDeviceManager, memory);
                if (nexaEvent != null)
                {
                    //Console.WriteLine("Dispatching Nexa event");
                    // Notify device
                    nexaEvent.Device.OnServiceEvent(nexaEvent);
                }
            }
            else if (packet.PacketType == Lighting5Protocol.PacketType)
            {
                EverflourishEvent everflourishEvent = Lighting5Protocol.HandlePackage(mDeviceManager, memory);
                if (everflourishEvent != null)
                {
                    //Console.WriteLine("Dispatching Everflourish event");
                    if (OnEverflourishEvent != null)
                        OnEverflourishEvent(this, everflourishEvent);
                }
            }
            else
            {
                Console.WriteLine("Discarding unhandled package. Size: {0}, Type: {1}, subType: {2}, sequence: {3}",
                    data.Length, packet.PacketType, packet.Subtype, packet.SequenceNumber);
                PrintRawPackage(data);
            }

            // Clean up the memory
            Marshal.FreeHGlobal(memory);
        }

        private void HandleRXResponsePackage(IntPtr memory)
        {
            var package = (RXRESPONSE)Marshal.PtrToStructure(memory, typeof(RXRESPONSE));

            if (package.subtype == RXRESPONSE.SUBTYPE_TRANSMITTER)
            {
                lock (mRadioLock)
                {
                    if (mLockOwned)
                        mRadioLock.Release();
                }

                if (package.msg == RXRESPONSE.RESPONSE_NAK || package.msg == RXRESPONSE.RESPONSE_NAK_ZERO)
                {
                    Console.WriteLine("Transmit failed with result: {0}", package.msg);
                }
            }
        }

        private bool SendPackage(Object package)
        {
            int packageSize = Marshal.SizeOf(package);

            byte[] data = new byte[packageSize];
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            
            Marshal.StructureToPtr(package, handle.AddrOfPinnedObject(), false);
            handle.Free();

            PrintRawPackage(data);

            if (!mSerialHelper.IsConnected)
            {
                Console.WriteLine("Discarding package since we're not connected");
                return false;
            }
            
            return mSerialHelper.WriteData(data);
        }

        private void PrintRawPackage(byte[] data)
        {
            Console.Write("Package data: 0x");
            foreach(var value in data)
                Console.Write(" {0:X2}", value);
            Console.WriteLine();
        }

        // Base for all package
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        private struct RFXPACKET
        {
            public byte PacketLength;
            public byte PacketType;
            public byte Subtype;
            public byte SequenceNumber;
        }

        // Interface response package
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14)]
        private struct IRESPONSE
        {
            public const byte TYPE = 0x01;

            public byte packetlength;
            public byte packettype;
            public byte subtype;
            public byte seqnbr;
            public byte cmnd;
            public byte msg1;
            public byte msg2;
            public byte msg3;
            public byte msg4;
            public byte msg5;
            public byte msg6;
            public byte msg7;
            public byte msg8;
            public byte msg9;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 5)]
        private struct RXRESPONSE
        {
            public const byte TYPE = 0x02;

            public const byte SUBTYPE_ERROR = 0x00;
            public const byte SUBTYPE_TRANSMITTER = 0x01;

            public const byte RESPONSE_ACK = 0x00;
            public const byte RESPONSE_ACK_DELAY = 0x01;
            public const byte RESPONSE_NAK = 0x02;
            public const byte RESPONSE_NAK_ZERO = 0x03;

            public byte packetlength;
            public byte packettype;
            public byte subtype;
            public byte seqnbr;
            public byte msg;
        };
    }
}
