using Module;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace ModuleBase.Archetypes
{
    public abstract class LampDeviceBase : DeviceBase
    {
        public class LampState : DeviceBase.DeviceState
        {
            public LampState()
            {
                Value = false;
                Level = 0.0f;
                Dimmable = false;
                Group = false;
            }

            public LampState(LampState state)
            : base (state)
            {
                Value = state.Value;
                Level = state.Level;
                Dimmable = state.Dimmable;
                Group = state.Group;
            }

            public bool Value { get; set; }
            public float Level { get; set; }
            public bool Dimmable { get; set; }
            public bool Group { get; set; }

            public new string Archetype { get { return "lamp"; } }
        };

        protected string mVoiceName;

        protected LampDeviceBase(LampState state, DeviceCreationInfo creationInfo)
        : base(state, creationInfo)
        {
            ForceSwitching = false;

            mVoiceName = creationInfo.Configuration.voiceName;
        }

        public override void ApplyState(DeviceBase.DeviceState state)
        {
            base.ApplyState(state);

            LampState currentState = (LampState)mState;
            LampState newState = (LampState)state;

            // Make sure we don't try to change these values.
            newState.Dimmable = currentState.Dimmable;
            newState.Group = currentState.Group;

            // Prioritize level changes which would indirectly affect Value changes.
            if (currentState.Dimmable
                && Math.Abs(newState.Level - currentState.Level) > 0.001f)
            {
                DimDevice(newState.Level);
            }
            else if (newState.Value != currentState.Value)
            {
                SwitchDevice(newState.Value);
            }
            else if (ForceSwitching)
            {
                if (newState.Level > 0.0f && newState.Level < 1.0f)
                    DimDevice(newState.Level);
                else
                    SwitchDevice(newState.Value);
            }
        }

        public override IList<VoiceCommand> GetVoiceCommands()
        {
            var commands = new List<VoiceCommand>();
            commands.Add(new VoiceCommand("turn on " + mVoiceName + " lamp", () => { SwitchDevice(true); }));
            commands.Add(new VoiceCommand("turn on the " + mVoiceName + " lamp", () => { SwitchDevice(true); }));
            commands.Add(new VoiceCommand("turn off " + mVoiceName + " lamp", () => { SwitchDevice(false); }));
            commands.Add(new VoiceCommand("turn off the " + mVoiceName + " lamp", () => { SwitchDevice(false); }));
            commands.Add(new VoiceCommand("dim " + mVoiceName + " lamp", () => { DimDevice(0.5f); }));
            commands.Add(new VoiceCommand("dim the " + mVoiceName + " lamp", () => { DimDevice(0.5f); }));

            return commands.AsReadOnly();
        }

        public void SwitchDevice(bool value)
        {
            if (OnSwitchDevice(value))
            {
                LampState state = (LampState)mState;
                state.Value = value;
                state.Level = value ? 1.0f : 0.0f;
            }
        }

        public void DimDevice(float level)
        {
            // Clamp level field
            level = Math.Min(Math.Max(level, 0.0f), 1.0f);

            if (OnDimDevice(level))
            {
                LampState state = (LampState)mState;
                state.Level = level;
                state.Value = level > 0.0f;
            }
        }

        protected abstract bool OnSwitchDevice(bool value);
        protected abstract bool OnDimDevice(float level);

        public bool Value { get { return ((LampState)mState).Value; } }
        public float Level { get { return ((LampState)mState).Level; } }
        public bool Group { get { return ((LampState)mState).Group; } }
        public bool Dimmable { get { return ((LampState)mState).Dimmable; } }

        protected bool ForceSwitching { get; set; }
    }
}
