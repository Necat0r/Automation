using Logging;
using Module;
using System;
using System.Net;
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

        private WakeOnLanService mWakeService;

        private ComputerState State { get { return (ComputerState)mState; } }

        private const int Timeout = 1000;

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
            Task.Run(() =>
            {
                try
                {
                    string url = string.Format("http://{0}{1}", State.Address, uri);
                    Log.Debug("Running web request: " + url);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "PUT";
                    request.Timeout = 1000;
                    request.ReadWriteTimeout = 1000;
                    request.ContentLength = 0;
                    request.GetResponse();

                    Log.Debug("Web request completed: " + url);
                }
                catch (Exception e)
                {
                    Log.Warning("Request failed with error: " + e.Message);
                }
            });
        }
    }
}
