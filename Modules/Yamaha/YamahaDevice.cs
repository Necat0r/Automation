using Logging;
using Module;
using System;
using System.Collections.Generic;
using System.Net;

namespace Yamaha
{
    public class YamahaDevice : DeviceBase
    {
        public class InputInfo
        {
            public InputInfo(string Name, string DisplayName, string VoiceName)
            {
                this.Name = Name;
                this.DisplayName = DisplayName;
                this.VoiceName = VoiceName;
            }

            public string Name;
            public string DisplayName;
            public string VoiceName;
        }

        public class YamahaState : DeviceBase.DeviceState
        {
            public YamahaState()
            {
                Model = "<unknown>";
                Power = false;
                Input = "";
                Volume = -450;
                Mute = false;
                Inputs = new List<InputInfo>();
            }

            public YamahaState(YamahaState state)
            : base(state)
            {
                Model = state.Model;
                Address = state.Address;
                Power = state.Power;
                Input = state.Input;
                Volume = state.Volume;
                Mute = state.Mute;
                Inputs = state.Inputs;
            }

            public string Model { get; set; }
            public string Address { get; set; }
            public bool Power { get; set; }
            public string Input { get; set; }
            public int Volume { get; set; }
            public bool Mute { get; set; }
            public List<InputInfo> Inputs { get; set; }
            public new string Archetype { get { return "receiver"; } }
        }

        private const int VOLUME_CHANGE = 50;

        dynamic mConfiguration;
        private YamahaService mService;
        private object mLock;

        private YamahaState State { get { return (YamahaState)mState; } }

        public YamahaDevice(DeviceCreationInfo creationInfo)
        : base(new YamahaState(), creationInfo)
        {
            mConfiguration = creationInfo.Configuration;

            mService = (YamahaService)creationInfo.ServiceManager.GetService(typeof(YamahaService));
            mLock = new object();

            YamahaState state = (YamahaState)mState;
            state.Address = creationInfo.Configuration.address;

            // Set up inputs
            foreach (var inputConfig in mConfiguration.inputs)
                state.Inputs.Add(new InputInfo(inputConfig.input, inputConfig.displayName, inputConfig.voiceName));


            // Note. Service will trigger a refresh of the device state, however, for a little while until the receiver responds, this device will have a potentially incorrect default state.
            mService.AddDevice(this);
        }

        public override IList<VoiceCommand> GetVoiceCommands()
        {
            var commands = new List<VoiceCommand>();
            commands.Add(new VoiceCommand("turn on receiver", () => { SetPower(true); }));
            commands.Add(new VoiceCommand("turn off receiver", () => { SetPower(false); }));
            commands.Add(new VoiceCommand("turn on the receiver", () => { SetPower(true); }));
            commands.Add(new VoiceCommand("turn off the receiver", () => { SetPower(false); }));
            commands.Add(new VoiceCommand("raise volume", () => { SetVolume(State.Volume + VOLUME_CHANGE); }));
            commands.Add(new VoiceCommand("lower volume", () => { SetVolume(State.Volume - VOLUME_CHANGE); }));
            commands.Add(new VoiceCommand("mute", () => { Mute(!State.Mute); }));

            // Rrgister input commands.
            foreach (var input in State.Inputs)
            {
                commands.Add(new VoiceCommand(String.Format("switch to {0}", input.VoiceName), () => { SetInput(input.Name); }));
                commands.Add(new VoiceCommand(String.Format("select {0} input", input.VoiceName), () => { SetInput(input.Name); }));
            }

            return commands.AsReadOnly();
        }

        public void SetPower(bool value)
        {
            IPAddress address = IPAddress.Parse(State.Address);
            mService.SetPower(this, value);

            lock (mLock)
                State.Power = value;
        }

        public void SetInput(string input)
        {
            // Validate it's configured
            var result = State.Inputs.Find(info => info.Name.ToLower() == input.ToLower());
            if (result == null)
            {
                Log.Warning("Invalid input '{0}' specified in device request", input);
                return;
            }

            Log.Debug("Setting input to: " + input);
            mService.SetInput(this, result.Name);

            lock (mLock)
                State.Input = input;
        }

        public void Mute(bool mute)
        {
            IPAddress address = IPAddress.Parse(State.Address);
            mService.Mute(this, mute);

            lock (mLock)
                State.Mute = mute;
        }

        public void SetVolume(int volume)
        {
            IPAddress address = IPAddress.Parse(State.Address);
            mService.SetVolume(this, volume);

            lock (mLock)
            {
                // Changing volume auto disables the mute.
                State.Mute = false;
                State.Volume = volume;
            }
        }

        public void SetScene(int scene)
        {
            Log.Error("YamahaDevice::SetScene() is not implemented!");
        }

        public override void ApplyState(DeviceBase.DeviceState state)
        {
            lock (mLock)
                base.ApplyState(state);

            var currentState = (YamahaState)mState;
            var newState = (YamahaState)state;

            if (newState.Power != currentState.Power)
                SetPower(newState.Power);

            if (newState.Input != currentState.Input)
                SetInput(newState.Input);

            if (newState.Mute != currentState.Mute)
                Mute(newState.Mute);

            if (newState.Volume != currentState.Volume)
                SetVolume(newState.Volume);
        }

        public void UpdateState(DeviceBase.DeviceState state)
        {
            // Replace internal state with the updated one.
            lock (mLock)
                mState = state;
        }

        public string Address { get { return ((YamahaState)mState).Address; } }
    }
}