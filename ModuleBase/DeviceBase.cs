using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Module
{
    public class DeviceBase
    {
        public class DeviceState
        {
            public DeviceState()
            { }

            public DeviceState(DeviceState state)
            {
                Name = state.Name;
                DisplayName = state.DisplayName;
                Archetype = state.Archetype;
                Type = state.Type;
            }

            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Archetype { get; set; }
            public string Type { get; set; }
        }

        public struct VoiceCommand
        {
            public delegate void VoiceDelegate();

            public VoiceCommand(string command, VoiceDelegate d)
                : this()
            {
                Command = command;
                Delegate = d;
            }

            public string Command { get; set; }
            public VoiceDelegate Delegate { get; set; }
        }

        protected DeviceState mState;

        protected DeviceBase(DeviceState state, DeviceCreationInfo creationInfo)
        {
            mState = state;
            mState.Name = creationInfo.Configuration.name;
            try
            {
                mState.DisplayName = creationInfo.Configuration.displayName;
            }
            catch (RuntimeBinderException) { }  // Optional

            mState.Type = GetType().ToString();
        }

        public virtual DeviceState CopyState()
        {
            return (DeviceState)Activator.CreateInstance(mState.GetType(), new object[] { mState });
        }

        public virtual void ApplyState(DeviceState state)
        {
            // Shouldn't change any of these post creation
            state.Name = mState.Name;
            state.DisplayName = mState.DisplayName;
            state.Archetype = mState.Archetype;

            if (!state.Type.Equals(mState.Type))
                throw new InvalidOperationException(string.Format("Type {0} of source state does not match type {1} of target state", state.Type, mState.Type));
        }

        public virtual IList<VoiceCommand> GetVoiceCommands() { return null; }

        public string Name
        {
            get { return mState.Name; }
        }

        public string DisplayName
        {
            get { return mState.DisplayName; }
        }

        public string Archetype
        {
            get { return mState.Archetype; }
        }

        public string Type
        {
            get { return mState.Type; }
        }
    }
}
