using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Support
{
    public class SerialHelper : IDisposable
    {
        private volatile bool mRunning = true;
        private AutoResetEvent mPortEvent = new AutoResetEvent(false);
        private ManualResetEvent mStopEvent = new ManualResetEvent(false);
        private EventWaitHandle[] mEvents;

        private string mName;
        private uint mPort;
        private int mBaudrate;

        private SerialPort mSerial;
        private Thread mReadThread;

        private List<SerialListener> mListeners = new List<SerialListener>();

        public string Name { get { return mName; } }
        public uint Port { get { return mPort; } }
        public uint Baudrate { get { return (uint)mBaudrate; } }

        public event EventHandler<EventArgs> OnConnected;
        public event EventHandler<EventArgs> OnDisconnected;
        public event EventHandler<byte[]> OnDataReceived;

        public interface SerialListener
        {
            void OnConnected();
            void OnDisconnected();
            void OnData(byte[] data);
        }

        public class Buffer
        {
            private byte[] mBuffer = null;

            public void Write(byte[] data)
            {
                var oldSize = (mBuffer != null ? mBuffer.Length : 0);
                var bufferSize = oldSize + data.Length;
                byte[] newBuffer = new byte[bufferSize];

                if  (mBuffer != null)
                    mBuffer.CopyTo(newBuffer, 0);
                data.CopyTo(newBuffer, oldSize);
                mBuffer = newBuffer;
            }

            public byte[] Consume(int length)
            {
                byte[] data = mBuffer.Take(length).ToArray();
                mBuffer = mBuffer.Skip(length).ToArray();

                return data;
            }

            public void Clear()
            {
                mBuffer = null;
            }

            public byte[] Data
            {
                get { return mBuffer; }
            }

            public bool IsEmpty
            {
                get
                {
                    if (mBuffer != null)
                        return mBuffer.Length == 0;

                    return true;
                }
            }
        }

        public SerialHelper(string name, uint port, uint baudrate)
        {
            mEvents = new EventWaitHandle[] { mPortEvent, mStopEvent };
            mName = name;
            mPort = port;
            mBaudrate = (int)baudrate;

            mSerial = new SerialPort("COM" + port, mBaudrate);
            mSerial.ReadTimeout = 100;

            mReadThread = new Thread(Read);
            mReadThread.Start();

            PortNotifier.PortAdded += new EventHandler<PortNotifier.PortAddedEventArgs>(PortAdded);
        }

        public void Dispose()
        {
            mPortEvent.Dispose();
            mStopEvent.Dispose();
            mSerial.Dispose();
        }
        
        ~SerialHelper()
        {
            PortNotifier.PortAdded -= new EventHandler<PortNotifier.PortAddedEventArgs>(PortAdded);
            mRunning = false;
            mStopEvent.Set();
            mReadThread.Join();
        }

        public void AddListener(SerialListener listener)
        {
            mListeners.Add(listener);
        }

        public void RemoveListener(SerialListener listener)
        {
            mListeners.Remove(listener);
        }

        private void PortAdded(object sender, PortNotifier.PortAddedEventArgs args)
        {
            if (args.Port == mPort && !IsConnected)
            {
                // Trigger a retry to connect to the port
                mPortEvent.Set();
            }
        }

        public bool IsConnected
        {
            get { return mSerial.IsOpen; }
        }

        private bool Connect()
        {
            if (IsConnected)
                return true;

            try
            {
                mSerial.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: Could not open port {1}, error: {2}", mName, mPort, e.Message);
                return false;
            }

            Console.WriteLine("{0}: Successfully opened port {1}", mName, mPort);
            foreach (var listener in mListeners)
                listener.OnConnected();

            if (OnConnected != null)
                OnConnected(this, EventArgs.Empty);

            return true;
        }

        // Force a reconnect to attempt to handle any mid-transfer errors
        public void Disconnect()
        {
            var connected = IsConnected;
            mSerial.Close();

            // Notify listeners
            if (connected)
            {
                foreach (var listener in mListeners)
                    listener.OnDisconnected();

                if (OnDisconnected != null)
                    OnDisconnected(this, EventArgs.Empty);
            }
        }

        public bool WriteData(byte[] data)
        {
            if (mSerial.IsOpen && data.Length > 0)
            {
                try
                {
                    lock (mSerial)
                    {
                        mSerial.Write(data, 0, data.Length);
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("{0}: Write timed out. Disconnecting", mName);
                    Disconnect();
                    return false;
                }
            }

            return true;
        }
        
        private void Read()
        {
            while (mRunning)
            {
                // Make sure we're connected
                if (!IsConnected)
                {
                    var result = Connect();
                    if (!result)
                    {
                        // Wait until something happens
                        EventWaitHandle.WaitAny(mEvents);
                        continue;
                    }
                }

                // Wait and try again in case there's nothing to read.
                if (mSerial.BytesToRead == 0)
                {
                    mStopEvent.WaitOne(100);
                    continue;
                }

                int bytesRead = 0;
                var data = new byte[mSerial.BytesToRead];

                try
                {
                    bytesRead = mSerial.Read(data, 0, mSerial.BytesToRead);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                    Console.WriteLine("{0}: Could not read data. Disconnecting...", mName);
                    Disconnect();
                    continue;
                }

                if (bytesRead > 0)
                {
                    Console.WriteLine("{0}: Read {1} bytes", mName, bytesRead);
                    Console.WriteLine("Str: {0}", System.Text.Encoding.UTF8.GetString(data));

                    foreach (var listener in mListeners)
                        listener.OnData(data);

                    if (OnDataReceived != null)
                        OnDataReceived(this, data);
                }
            }

            // Clean up
            if (IsConnected)
            {
                Console.WriteLine("{0}: Closing port {1}", mName, mPort);
                Disconnect();
            }
        }
    }
}
