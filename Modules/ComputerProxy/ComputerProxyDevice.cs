﻿using Module;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using WakeOnLan;

namespace ComputerProxy
{
    public class ComputerProxyDevice : ProxyDevice
    {
        protected class ComputerState : DeviceBase.DeviceState
        {
            public ComputerState()
            { }

            public ComputerState(ComputerState state)
            : base(state)
            {
                Address = state.Address;
                MacAddress = state.MacAddress;
                Power = state.Power;
                MonitorPower = state.MonitorPower;
            }

            public string Address { get; set; }
            public string MacAddress { get; set; }
            public bool Power { get; set; }
            public bool MonitorPower { get; set; }
            public new string Archetype { get { return "desktop"; } }
        };

        private class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest w = base.GetWebRequest(address);
                w.Timeout = 1000;
                return w;
            }
        }

        private WakeOnLanService mWakeService;

        private ComputerState State { get { return (ComputerState)mState; } }

        public ComputerProxyDevice(DeviceCreationInfo creationInfo)
            : base(new ComputerState(), creationInfo)
        {
            State.Address = creationInfo.Configuration.address;
            State.MacAddress = creationInfo.Configuration.macaddress;

            mWakeService = (WakeOnLanService)creationInfo.ServiceManager.GetService(typeof(WakeOnLanService));
        }

        public void SetPower(bool power)
        {
            if (power)
            {
                if (mWakeService != null)
                    mWakeService.Wake(State.MacAddress);
            }
            else
            {
                RunRequest("/power/standby");
            }
            State.Power = power;
        }

        public void SetMonitorPower(bool power)
        {
            RunRequest("/power/monitor?value=" + power);
            
            // TODO - Read this back from computer instead.
            State.MonitorPower = power;
        }

        public override void ApplyState(DeviceState state)
        {
            base.ApplyState(state);

            var newState = (ComputerState)state;
            var currentState = (ComputerState)mState;

            if (newState.Power != currentState.Power)
                SetPower(newState.Power);

            if (newState.MonitorPower != currentState.MonitorPower)
                SetMonitorPower(newState.MonitorPower);
        }

        private void RunRequest(string uri)
        {
            Task.Run(async () =>
            {
                using (var client = new TimeoutWebClient())
                {
                    try
                    {
                        string url = string.Format("http://{0}{1}", State.Address, uri);
                        await client.UploadStringTaskAsync(url, "PUT", "");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Request failed with error: " + e.Message);
                    }
                }
            });
        }
    }
}
