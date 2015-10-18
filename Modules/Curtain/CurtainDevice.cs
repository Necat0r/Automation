using Module;
using ModuleBase.Archetypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Curtain
{
    public class CurtainDevice : CurtainDeviceBase
    {
        public enum Key
        {
            Down = 0,
            Stop = 1,
            Up = 2,
        }

        protected new class CurtainState : CurtainDeviceBase.CurtainState
        {
            public CurtainState()
            {
                Action = Key.Stop;
                Position = 0.0f;
            }

            public CurtainState(CurtainState state)
                : base(state)
            {
                Action = state.Action;
                Channel = state.Channel;
                Position = state.Position;
            }

            public Key Action { get; set; }
            public int Channel { get; set; }
            public float Position { get; set; }
        }

        private readonly int DEVICE_ID = 0;

        private CurtainService mService;

        private Key mLastCommand = Key.Stop;
        private DateTime mLastTime = DateTime.Now;
        private string mVoiceName;

        public CurtainDevice(DeviceCreationInfo creationInfo)
        : base(new CurtainState(), creationInfo)
        {
            mService = (CurtainService)creationInfo.ServiceManager.GetService(typeof(CurtainService));

            CurtainState state = (CurtainState)mState;
            state.Channel = int.Parse(creationInfo.Configuration.channel);

            mVoiceName = creationInfo.Configuration.voiceName;

/*
            // TOTO - Look into this in detail.
            // Somehow we're getting garbage in the first submission with MS serial implementation.
            // No issues in the previous python implementation, but required beefing up error handling on the arduino side as well
            // as adding an extra flush from the c# side.
            byte[] dummy = new byte[] { (byte)'\n' };
            mService.SendData(dummy);
 */
        }

        #region Device interface
        public override void Up()
        {
            PredictPosition();
            RunCommand(Key.Up);
        }

        public override void Stop()
        {
            PredictPosition();
            RunCommand(Key.Stop);
        }

        public override void Down()
        {
            PredictPosition();
            RunCommand(Key.Down);
        }

        public override float Position
        {
            get
            {
                CurtainState state = (CurtainState)mState;
                return state.Position;
            }
        }
        #endregion

        public override void ApplyState(DeviceBase.DeviceState state)
        {
            base.ApplyState(state);

            CurtainState currentState = (CurtainState)mState;
            CurtainState newState = (CurtainState)state;

            Debug.Assert(newState.Channel == currentState.Channel);

            // FIXME - Disable prediction since we're getting out of sync somehow.
            //if (newState.Action != currentState.Action)
            {
                switch (newState.Action)
                {
                    case Key.Up:
                        Up();
                        break;
                    case Key.Stop:
                        Stop();
                        break;
                    case Key.Down:
                        Down();
                        break;
                }
            }
        }

        public override IList<VoiceCommand> GetVoiceCommands()
        {
            var commands = new List<VoiceCommand>();
            commands.Add(new VoiceCommand("raise curtain in " + mVoiceName, () => { Up(); }));
            commands.Add(new VoiceCommand("raise " + mVoiceName + " curtain", () => { Up(); }));
            commands.Add(new VoiceCommand("lower curtain in " + mVoiceName, () => { Down(); }));
            commands.Add(new VoiceCommand("lower " + mVoiceName + " curtain", () => { Down(); }));
            commands.Add(new VoiceCommand("stop curtain in " + mVoiceName, () => { Stop(); }));
            commands.Add(new VoiceCommand("stop " + mVoiceName + " curtain", () => { Stop(); }));

            return commands.AsReadOnly();
        }

        private void PredictPosition()
        {
            CurtainState state = (CurtainState)mState;

            const int syncDuration = 18500;

            DateTime currTime = DateTime.Now;
            TimeSpan diff = mLastTime - currTime;
            mLastTime = currTime;

            // Fixme. Add better prediction.
            if (diff.Milliseconds > syncDuration)
            {
                if (mLastCommand == Key.Up)
                    state.Position = 1.0f;
                else if (mLastCommand == Key.Down)
                    state.Position = 0.0f;
            }
        }

        private void RunCommand(Key key)
        {
            CurtainState state = (CurtainState)mState;

            // size, payload[size]
            byte[] data = new byte[] { (byte)3, (byte)DEVICE_ID, (byte)state.Channel, (byte)key };
            mService.SendData(data);

            mLastCommand = key;
            state.Action = key;
        }
    }
}
