using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Support
{
    public class PortNotifier
    {
        public sealed class PortAddedEventArgs : EventArgs
        {
            public int Port { get; private set; }
            public PortAddedEventArgs(int port) { Port = port; }
        }

        public static event EventHandler<PortAddedEventArgs> PortAdded;

        public static void Init()
        {
            if (sWindow == null)
                sWindow = new MessageWindow();
        }
        private static MessageWindow sWindow;

        private sealed class MessageWindow : NativeWindow
        {
            const int WM_DEVICECHANGE = 0x219;
            const int DBT_DEVICEARRIVAL = 0x8000;
            const int DBT_DEVTYP_PORT = 0x03;

            public MessageWindow()
            {
                CreateParams createParams = new CreateParams();
                CreateHandle(createParams);
            }

            [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
            protected override void WndProc(ref Message m)
            {
                int changeType = (int)m.WParam;
                if (m.Msg == WM_DEVICECHANGE)
                {
                    if (changeType == DBT_DEVICEARRIVAL)
                    {
                        DEV_BROADCAST_HDR header = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));

                        if (header.DeviceType == DBT_DEVTYP_PORT)
                        {
                            string portName = Marshal.PtrToStringAuto((IntPtr)((long)m.LParam + 12));
                            Console.WriteLine("Port {0} was added", portName);

                            if (portName.StartsWith("COM"))
                            {
                                int port = Convert.ToInt32(portName.Substring(3));

                                // Notify listeners
                                if (PortAdded != null)
                                    PortAdded(this, new PortAddedEventArgs(port));
                            }
                        }
                    }
                }

                base.WndProc(ref m);
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DEV_BROADCAST_HDR
            {
                internal int Size;
                internal int DeviceType;
                internal int Reserved;
            }

            //[StructLayout(LayoutKind.Sequential)]
            //private struct DEV_BROADCAST_PORT
            //{
            //    internal int Size;
            //    internal int DeviceType;
            //    internal int Reserved;
            //    internal short Name;
            //}
        }
    }
}