using Module;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Bluetooth
{
    public class BluetoothService : ServiceBase, IDisposable
    {
        private TimeSpan AWAY_TIMEOUT = new TimeSpan(1, 0, 0);
        private TimeSpan SCAN_INTERVAL_AWAY = new TimeSpan(0, 0, 20);
        private TimeSpan SCAN_INTERVAL_PRESENT = new TimeSpan(0, 30, 1);    // Keep a multiple of this slightly longer than AWAY_TIMEOUT to prevent evaulating 1sec before the timeout.
        private TimeSpan INTENSIVE_SCAN_DURATION = new TimeSpan(0, 1, 0);

        private class DeviceInfo
        {
            public BluetoothDevice Device;
            public DateTime LastTime;
            public DateTime NextScan = new DateTime();
            public bool FirstScan = true;

            public DeviceInfo(BluetoothDevice device)
            {
                Device = device;
                LastTime = DateTime.Now;
            }
        }

        private Thread mThread;
        private ManualResetEvent mStopEvent = new ManualResetEvent(false);
        private AutoResetEvent mDeviceEvent = new AutoResetEvent(false);
        private EventWaitHandle[] mEvents;

        private volatile bool mRunning = true;
        private volatile bool mIntensiveScan = false;

        private BluetoothHelper mBluetooth;
        private List<DeviceInfo> mDevices = new List<DeviceInfo>();
        private string[] mDeviceNames;

        private class BluetoothHelper : IDisposable
        {
            public BluetoothHelper()
            {
                NativeMethods.bt_init();
            }

            public void Dispose()
            {
                NativeMethods.bt_shutdown();
            }

            public string[] GetDevices()
            {
                uint deviceCount = NativeMethods.bt_getDeviceCount();

                if (deviceCount == 0)
                    return null;

                string[] devices = new string[deviceCount];

                for (uint i = 0; i < deviceCount; ++i)
                {
                    IntPtr namePtr = NativeMethods.bt_getDeviceName(i);
                    devices[i] = Marshal.PtrToStringAnsi(namePtr);
                }

                return devices;
            }

            public void RefreshDevices()
            {
                NativeMethods.bt_refreshDevices();
            }

            public void UpdateDevice(string name)
            {
                IntPtr namePtr = Marshal.StringToCoTaskMemAnsi(name);
                NativeMethods.bt_updateDevice(namePtr);
                Marshal.FreeCoTaskMem(namePtr);
            }

            public bool InRange(string name)
            {
                IntPtr namePtr = Marshal.StringToCoTaskMemAnsi(name);
                bool result = NativeMethods.bt_inRange(namePtr);
                Marshal.FreeCoTaskMem(namePtr);

                return result;
            }
        }

        public BluetoothService(string name, ServiceCreationInfo info)
            : base("bluetooth", info)
        {
            mBluetooth = new BluetoothHelper();

            // Refresh available devices
            mBluetooth.RefreshDevices();
            mDeviceNames = mBluetooth.GetDevices();
            Console.WriteLine("Bluetooth devices: {0}", mDeviceNames);

            // Set up scan thread.
            mEvents = new EventWaitHandle[] { mStopEvent, mDeviceEvent };
            mThread = new Thread(_searchThread);
            mThread.Start();
        }

        public void Dispose()
        {
            // Shut down thread
            if (mThread != null)
            {
                mRunning = false;
                mStopEvent.Set();
                mThread.Join(10000);
                mThread = null;
            }

            mDeviceEvent.Dispose();
            mStopEvent.Dispose();
            mBluetooth.Dispose();
        }

        private void _searchThread()
        {
            Console.WriteLine("Starting Bluetooth search thread");

            // Avoid the initial lock contestion for mDevices
            Thread.Sleep(5000);

            DateTime intensiveScanTime = DateTime.Now;

            while (mRunning)
            {
                mBluetooth.RefreshDevices();

                DateTime currTime = DateTime.Now;
                DateTime nextScan = currTime + new TimeSpan(10, 0, 0);  // Just some high dummy value to diff against

                if (mIntensiveScan)
                {
                    Console.WriteLine("Starting intensive scan");
                    mIntensiveScan = false;
                    intensiveScanTime = currTime + INTENSIVE_SCAN_DURATION;
                }

                lock (mDevices)
                {
                    foreach (var info in mDevices)
                    {
                        string btName = info.Device.BtName;
                        BluetoothDevice device = info.Device;

                        // Scan for device
                        mBluetooth.UpdateDevice(btName);
                        bool inRange = mBluetooth.InRange(btName);

                        // Update last time device was seen.
                        if (inRange)
                            info.LastTime = currTime;

                        // Initial notify to set up device state properly, but don't notify it's listeners
                        if (info.FirstScan)
                        {
                            info.FirstScan = false;

                            // Notify device of it's initial state.
                            if (inRange)
                                Console.WriteLine("Bluetooth: device {0} initially present", btName);
                            else
                                Console.WriteLine("Bluetooth: device {0} initially away", btName);
                            device.OnStatusUpdate(inRange, true);
                        }
                        else if (device.InRange && !inRange && (currTime - info.LastTime) >= AWAY_TIMEOUT)
                        {
                            // Lost device
                            Console.WriteLine("Lost device: {0}", btName);
                            info.Device.OnStatusUpdate(inRange);
                        }
                        else if (!device.InRange && inRange)
                        {
                            // Found device
                            Console.WriteLine("Found device: {0}", btName);
                            info.Device.OnStatusUpdate(inRange);
                        }

                        TimeSpan delayTime;
                        // Delay dependent on current state
                        if (device.InRange)
                            delayTime = SCAN_INTERVAL_PRESENT;
                        else
                            delayTime = SCAN_INTERVAL_AWAY;
                        info.NextScan = currTime + delayTime;

                        if (info.NextScan < nextScan)
                            nextScan = info.NextScan;
                    }
                }

                // Running intensive scan so don't sleep just yet.
                if (currTime < intensiveScanTime)
                    continue;

                EventWaitHandle.WaitAny(mEvents, nextScan - currTime);
            }
        }

        public void AddDevice(BluetoothDevice device)
        {
            if (mDeviceNames == null || mDeviceNames.Length == 0)
            {
                Console.WriteLine("WARNING: No devices available on this machine. Please pair '{0}' with this machine.", device.BtName);
                return;
            }

            // Make sure the device name exist
            string name = Array.Find(mDeviceNames, x => x.Equals(device.BtName));
            if (name == null)
                throw new Exception("Device name not found. Has the device not been paired to this machine?");

            lock (mDevices)
            {
                mDevices.Add(new DeviceInfo(device));
            }
            mDeviceEvent.Set();
        }

        public void RemoveDevice(BluetoothDevice device)
        {
            foreach (var deviceInfo in mDevices)
            {
                if (deviceInfo.Device == device)
                {
                    lock (mDevices)
                    {
                        mDevices.Remove(deviceInfo);
                    }
                    mDeviceEvent.Set();
                    break;
                }
            }
        }

        // At this point we cannot force an update for only a single device. The closest is to use the device event to wakt up the scan thread and check all devices...
        public void ForceDeviceUpdate(BluetoothDevice device)
        {
            mIntensiveScan = true;
            mDeviceEvent.Set();
        }
    }

    internal static class NativeMethods
    {
        [DllImport("SupportDll.dll")]
        internal static extern bool bt_init();

        [DllImport("SupportDll.dll")]
        internal static extern void bt_refreshDevices();

        [DllImport("SupportDll.dll")]
        internal static extern uint bt_getDeviceCount();

        [DllImport("SupportDll.dll")]
        internal static extern IntPtr bt_getDeviceName(uint index);

        [DllImport("SupportDll.dll")]
        internal static extern void bt_updateDevice(IntPtr deviceName);

        [DllImport("SupportDll.dll")]
        internal static extern void bt_shutdown();

        [DllImport("SupportDll.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool bt_inRange(IntPtr deviceName);

        [DllImport("SupportDll.dll")]
        internal static extern int bt_getLastError(IntPtr deviceName);
    }
}
