﻿using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Module;

namespace PowerControl
{
    public class PowerControlService : ServiceBase
    {
        private IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private UInt32 WM_SYSCOMMAND = 0x0112;
        private IntPtr SC_MONITORPOWER = new IntPtr(0xF170);
        private IntPtr ON = new IntPtr(-1);
        private IntPtr OFF = new IntPtr(2);

        public PowerControlService(ServiceCreationInfo info)
            : base("power", info)
        { }

        [ServicePutContract("monitor?{value}")]
        public object OnMonitorRequest(string value)
        {
            // TODO - Shouldn't need to parse here.
            if (bool.Parse(value))
                NativeMethods.PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, ON);
            else
                NativeMethods.PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, OFF);
            return value;
        }

        [ServicePutContract("standby")]
        public void OnSuspend()
        {
            Application.SetSuspendState(PowerState.Suspend, true, false);
        }
    }

    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern int PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}
