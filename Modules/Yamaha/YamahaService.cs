using Logging;
using Module;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using System.Linq;
//using System.Data.Linq;
//using System.Data.Entity;


namespace Yamaha
{
    public class YamahaService : ServiceBase
    {
        //private IPAddress mAddress;
        private const string CONTROL_URI = "/YamahaRemoteControl/ctrl";

        private const int REFRESH_INTERVAL = 5000;
        private const string REQUEST_PARAMETERS = "<YAMAHA_AV cmd=\"GET\"><System><Config>GetParam</Config></System></YAMAHA_AV>";
        private const string REQUEST_MAIN_ZONE = "<YAMAHA_AV cmd=\"GET\"><Main_Zone><Basic_Status>GetParam</Basic_Status></Main_Zone></YAMAHA_AV>";

        private HashSet<YamahaDevice> mDevices;

        public YamahaService(string name, ServiceCreationInfo info)
            : base("yamaha", info)
        {
            mDevices = new HashSet<YamahaDevice>();
        }

        public void AddDevice(YamahaDevice device)
        {
            if (mDevices.Contains(device))
                return;

            mDevices.Add(device);

            Task.Run(async () => {

                // Get initial device parameters
                await GetDeviceParameters(device);

                // Start refreshing device
                while (true)
                {
                    await RefreshDeviceAsync(device);

                    // TODO - Potentially trigger push notifications to clients...

                    await Task.Delay(REFRESH_INTERVAL);
                }
            });
        }

        // Run a request to alter the device's state
        private void RunRequest(YamahaDevice device, string body)
        {
            Task.Run(async () => {
                // Kick off our command.
                await RunCommandAsync(device, body);

                // Delay 100ms so crappy receiver has a chance to update its data before we do our refresh. :(
                await Task.Delay(100);

                // Refresh device once it has completed.
                await RefreshDeviceAsync(device);
                }
            );
        }

        // Run an asynchronous command to the receiver.
        public async Task<string> RunCommandAsync(YamahaDevice device, string request)
        {
            using (var client = new WebClient())
            {
                try
                {
                    string url = "http://" + device.Address + CONTROL_URI;
                    return await client.UploadStringTaskAsync(url, "POST", request);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Request failed with error: " + e.Message);
                }

                return default(string);
            }
        }

        public async Task GetDeviceParameters(YamahaDevice device)
        {
            string parameters = await RunCommandAsync(device, REQUEST_PARAMETERS);

            if (parameters == null)
                return;

            /*
            <YAMAHA_AV rsp="GET" RC="0">
                <System>
                    <Config>
                        <Model_Name>RX-V673</Model_Name>
                        <System_ID>0B816293</System_ID>
                        <Version>1.64/2.06</Version>
                        <Feature_Existence>
                            <Main_Zone>1</Main_Zone>
                            <Zone_2>1</Zone_2>
                            <Zone_3>0</Zone_3>
                            <Zone_4>0</Zone_4>
                            <Tuner>1</Tuner>
                            <HD_Radio>0</HD_Radio>
                            <Rhapsody>0</Rhapsody>
                            <Napster>1</Napster>
                            <SiriusXM>0</SiriusXM>
                            <Pandora>0</Pandora>
                            <SERVER>1</SERVER>
                            <NET_RADIO>1</NET_RADIO>
                            <USB>1</USB>
                            <iPod_USB>1</iPod_USB>
                            <AirPlay>1</AirPlay>
                        </Feature_Existence>
                        <Name>
                            <Input>
                                <HDMI_1>HDMI1</HDMI_1>
                                <HDMI_2>HDMI2</HDMI_2>
                                <HDMI_3>HDMI3</HDMI_3>
                                <HDMI_4>HDMI4</HDMI_4>
                                <HDMI_5>HDMI5</HDMI_5>
                                <AV_1>AV1</AV_1>
                                <AV_2>AV2</AV_2>
                                <AV_3>AV3</AV_3>
                                <AV_4>AV4</AV_4>
                                <AV_5>AV5</AV_5>
                                <AV_6>AV6</AV_6>
                                <V_AUX>V-AUX</V_AUX>
                                <AUDIO_1>AUDIO1</AUDIO_1>
                                <AUDIO_2>AUDIO2</AUDIO_2>
                                <USB>USB</USB>
                            </Input>
                        </Name>
                    </Config>
                </System>
            </YAMAHA_AV>
            */

            XmlDocument document = new XmlDocument();
            document.LoadXml(parameters);

            YamahaDevice.YamahaState state = (YamahaDevice.YamahaState)device.CopyState();

            // Model name
            var modelNode = document.SelectSingleNode("YAMAHA_AV/System/Config/Model_Name");
            if (modelNode != null)
                state.Model = modelNode.InnerText;

            // Inputs
            XmlNodeList inputNodes = document.SelectNodes("YAMAHA_AV/System/Config/Name/Input/*");
            if (inputNodes.Count > 0)
            {
                var inputs = (from input in inputNodes.Cast<XmlNode>() select input.InnerText).ToList<string>();

                Log.Debug("Available inputs:");
                foreach (var input in inputs)
                    Log.Debug("\tInput: {0}", input);

                // Validate configuration. Better late than never...
                var invalidKeys = new HashSet<dynamic>();

                foreach (var info in state.Inputs)
                {
                    var result = inputs.Find((name) => name.ToLower() == info.Name.ToLower());
                    if (result == null)
                    {
                        Log.Error("Configured input '{0}' is invalid. Disabling input.", info.Name);
                        invalidKeys.Add(info);
                    }

                    // Enforce correct casing
                    info.Name = result;
                }

                // Purge incorrect configs
                if (invalidKeys.Count > 0)
                {
                    Log.Error("Removing {0} incorrect inputs", invalidKeys.Count);
                    foreach (var key in invalidKeys)
                        state.Inputs.Remove(key);
                }
            }

            device.UpdateState(state);
        }

        public void RefreshDevice(YamahaDevice device)
        {
            Task.Run(async () => {
                await RefreshDeviceAsync(device);
            });
        }

        public async Task RefreshDeviceAsync(YamahaDevice device)
        {
            string result = await RunCommandAsync(device, REQUEST_MAIN_ZONE);

            if (result == null || result.Length == 0)
                return;

            XmlDocument document = new XmlDocument();
            try
            {
                document.LoadXml(result);
            }
            catch (XmlException e)
            {
                Log.Warning("Exception loading device status xml: {0}", e.Message);
                return;
            }

            bool dirty = false;
            YamahaDevice.YamahaState state = (YamahaDevice.YamahaState)device.CopyState();

            XmlNode powerNode = document.SelectSingleNode("YAMAHA_AV/Main_Zone/Basic_Status/Power_Control/Power");
            if (powerNode != null)
            {
                bool power = state.Power;
                state.Power = powerNode.InnerText == "On";
                dirty |= power != state.Power;
            }

            XmlNode volumeNode = document.SelectSingleNode("YAMAHA_AV/Main_Zone/Basic_Status/Volume/Lvl/Val");
            if (volumeNode != null)
            {
                int volume = state.Volume;
                state.Volume = int.Parse(volumeNode.InnerText);
                dirty |= volume != state.Volume;
            }

            XmlNode muteNode = document.SelectSingleNode("YAMAHA_AV/Main_Zone/Basic_Status/Volume/Mute");
            if (muteNode != null)
            {
                bool mute = state.Mute;
                state.Mute = muteNode.InnerText == "On";
                dirty |= mute != state.Mute;
            }

            XmlNode inputNode = document.SelectSingleNode("YAMAHA_AV/Main_Zone/Basic_Status/Input/Input_Sel");
            if (inputNode != null)
            {
                string input = state.Input;
                state.Input = inputNode.InnerText;
                dirty |= input != state.Input;
            }

            if (dirty)
                device.UpdateState(state);
        }

        public void SetPower(YamahaDevice device, bool value)
        {
            if (value)
            {
                // Only switch on main zone
                string body = "<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Power_Control><Power>On</Power></Power_Control></Main_Zone></YAMAHA_AV>";
                RunRequest(device, body);
            }
            else
            {
                // Make sure we shut down everything
                string body = "<YAMAHA_AV cmd=\"PUT\"><System><Power_Control><Power>Standby</Power></Power_Control></System></YAMAHA_AV>";
                RunRequest(device, body);
            }

            //RefreshDevice(device);
        }

        public void Mute(YamahaDevice device, bool mute)
        {
            string body = string.Format("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Volume><Mute>{0}</Mute></Volume></Main_Zone></YAMAHA_AV>", mute ? "On" : "Off");
            RunRequest(device, body);
            //RefreshDevice(device);
        }

        public void SetVolume(YamahaDevice device, int volume)
        {
            string body = string.Format("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Volume><Lvl><Val>{0}</Val><Exp>1</Exp><Unit>dB</Unit></Lvl></Volume></Main_Zone></YAMAHA_AV>", volume);
            RunRequest(device, body);
            //RefreshDevice(device);
        }

        public void SetScene(YamahaDevice device, int scene)
        {
            string body = string.Format("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Scene><Scene_Load>{0}</Scene_Load></Scene></Main_Zone></YAMAHA_AV>", (scene + 1));
            RunRequest(device, body);
            //RefreshDevice(device);
        }

        public void SetInput(YamahaDevice device, string input)
        {
            string body = string.Format("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>{0}</Input_Sel></Input></Main_Zone></YAMAHA_AV>", input);
            RunRequest(device, body);
            //RefreshDevice(device);
        }
    }
}
