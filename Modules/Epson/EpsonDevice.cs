using Module;
using Support;
using System;
using System.Collections.Generic;

namespace Epson
{
    public class EpsonDevice : DeviceBase, SerialHelper.SerialListener, IDisposable
    {
        public enum InputSource
        {
            Unknown,
            Hdmi
        };

        public enum Key
        {
            Enter,
            Menu,
            Up,
            Down,
            Left,
            Right
        };

        protected class EpsonState : DeviceBase.DeviceState
        {
            public EpsonState()
            {
                WarmingUp = false;
                Source = InputSource.Unknown;
            }

            public EpsonState(EpsonState state)
            : base(state)
            {
                Power = state.Power;
                WarmingUp = state.WarmingUp;
                Source = state.Source;
                LampHours = state.LampHours;
            }

            public bool Power { get; set; }
            public bool WarmingUp { get; set; }
            public InputSource Source { get; set; }
            public int LampHours { get; set; }
            public new string Archetype { get { return "projector"; } }
        }
             

        #region Private state
        private enum Command
        {
            Init,
            GetError,
            GetPower,
            SetPower,
            GetSource,
            SetSource,
            GetLampHours,
            SendKey,
        }

        private class CommandInfo
        {
            public Command Command;
            public bool Power;
            public Key Key;
            public InputSource Source;

            public CommandInfo(Command command)
            {
                Command = command;
            }

            public override bool Equals(object x)
            {
                CommandInfo rhs = x as CommandInfo;

                return this.Command == rhs.Command &&
                    this.Power == rhs.Power &&
                    this.Key == rhs.Key &&
                    this.Source == rhs.Source;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private SerialHelper mSerialHelper;
        private SerialHelper.Buffer mBuffer = new SerialHelper.Buffer();

        //private bool mPoweredOn = false;
        //private InputSource mSource = InputSource.Unknown;
        //private int mLampHours;

        private CommandInfo mRunningCommand;
        private object mCommandLock = new object();
        private List<CommandInfo> mCommandQueue = new List<CommandInfo>();

        private EpsonState State { get { return (EpsonState)mState; } }

        #endregion

        #region Interface
        public EpsonDevice(DeviceCreationInfo creationInfo)
            : base(new EpsonState(), creationInfo)
        {
            int port = int.Parse(creationInfo.Configuration.port);
            mSerialHelper = new SerialHelper("epson", (uint)port, 9600);
            mSerialHelper.AddListener(this);
        }

        public void Dispose()
        {
            mSerialHelper.Dispose();
        }

        public void PowerOn()
        {
            if (!State.Power)
                QueueCommand(new CommandInfo(Command.SetPower) { Power = true });
            State.WarmingUp = true;
        }

        public void PowerOff()
        {
            if (State.Power)
                QueueCommand(new CommandInfo(Command.SetPower) { Power = false });
        }

        public bool Power
        {
            get { return State.Power; }
        }

        public InputSource Source
        {
            get { return State.Source; }

            set
            {
                int enumCount = Enum.GetNames(typeof(InputSource)).Length;
                int sourceInt = (int)value;

                if (sourceInt < 0 || sourceInt >= enumCount)
                    throw new IndexOutOfRangeException("Source index out of bounds");

                if (value != State.Source)
                    QueueCommand(new CommandInfo(Command.SetSource) { Source = InputSource.Hdmi });
            }
        }

        public int LampHours
        {
            get { return State.LampHours; }
        }

        public void SendKey(Key key)
        {
            int enumCount = Enum.GetNames(typeof(Key)).Length;
            int keyInt = (int)key;

            if (keyInt < 0 || keyInt >= enumCount)
                throw new IndexOutOfRangeException("Key index is out of range. Key: " + key);

            QueueCommand(new CommandInfo(Command.SendKey) { Key = key });
        }

        public override void ApplyState(DeviceBase.DeviceState state)
        {
            base.ApplyState(state);

            EpsonState currentState = (EpsonState)mState;
            EpsonState newState = (EpsonState)state;

            if (newState.Power != currentState.Power)
            {
                if (newState.Power)
                    PowerOn();
                else
                    PowerOff();
            }
            else if (newState.Source != currentState.Source)
            {
                Source = newState.Source;
            }
        }

        public override IList<VoiceCommand> GetVoiceCommands()
        {
            var commands = new List<VoiceCommand>();
            commands.Add(new VoiceCommand("turn on projector", () => { PowerOn(); }));
            commands.Add(new VoiceCommand("turn off projector", () => { PowerOff(); }));

            return commands.AsReadOnly();
        }
        #endregion

        private void QueueCommand(CommandInfo newInfo)
        {
            lock (mCommandQueue)
            {
                CommandInfo existingInfo = mCommandQueue.Find(
                    (CommandInfo info) =>
                    {
                        return info.Equals(newInfo);
                    });

                // Discard new request to add same command
                if (existingInfo != null)
                    return;

                mCommandQueue.Add(newInfo);
            }

            // Try to run command directly if there's no other 
            RunCommand();
        }

        private void RunCommand()
        {
            lock (mCommandLock)
            {
                // Check if there's an active command
                if (mRunningCommand != null)
                    return;

                lock (mCommandQueue)
                {
                    if (mCommandQueue.Count == 0)
                        return;

                    mRunningCommand = mCommandQueue[0];
                    mCommandQueue.RemoveAt(0);
                }
            }

            string commandString = GetCommandString(mRunningCommand);
            SendString(commandString);
        }

        private void SendString(string command)
        {
            if (command.Length > 1)
                Console.WriteLine("EpsonDevice name=" + Name + " - Sending command: " + command);

            byte[] data = System.Text.Encoding.ASCII.GetBytes(command);
            mSerialHelper.WriteData(data);
        }

        private string GetCommandString(CommandInfo info)
        {
            string commandString;
            switch (mRunningCommand.Command)
            {
                case Command.Init:
                    commandString = "";
                    break;

                case Command.GetError:
                    commandString = "ERR?";
                    break;

                case Command.GetPower:
                    commandString = "PWR?";
                    break;

                case Command.SetPower:
                    commandString = "PWR " + (mRunningCommand.Power ? "ON" : "OFF");
                    break;

                case Command.GetSource:
                    commandString = "SOURCE?";
                    break;

                case Command.SetSource:
                    commandString = "SOURCE " + GetSourceString(info.Source);
                    break;

                case Command.GetLampHours:
                    commandString = "LAMP?";
                    break;

                case Command.SendKey:
                    commandString = "KEY " + GetKeyString(info.Key);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return commandString + "\r";
        }

        private string GetKeyString(Key key)
        {
            switch (key)
            {
                case Key.Enter:
                    return "01";

                case Key.Menu:
                    return "03";

                case Key.Up:
                    return "35";

                case Key.Down:
                    return "36";

                case Key.Left:
                    return "37";

                case Key.Right:
                    return "38";

                default:
                    throw new NotImplementedException();
            }
        }

        private string GetSourceString(InputSource source)
        {
            switch (source)
            {
                case InputSource.Hdmi:
                    return "A0";

                default:
                    throw new NotImplementedException();
            }
        }

        #region Serial interface
        public void OnConnected()
        {
            // Queue up get requests for all attributes.
            // start with \r to get a : to start with
            lock (mBuffer)
                mBuffer.Clear();

            // Send initial 0x0D to trigger projector to send back a ":" sign
            QueueCommand(new CommandInfo(Command.Init));

            // Queue to get initial state
            QueueCommand(new CommandInfo(Command.GetLampHours));
            QueueCommand(new CommandInfo(Command.GetPower));
        }

        public void OnDisconnected()
        {
            Console.WriteLine("Got disconnected from projector");

            // Purge out buffer.
            lock (mBuffer)
                mBuffer.Clear();
        }

        public void OnData(byte[] data)
        {
            Console.WriteLine("Got data " + System.Text.Encoding.ASCII.GetString(data));

            bool responseFound = false;
            byte[] package = null;

            // Add onto buffer. 
            lock (mBuffer)
            {
                mBuffer.Write(data);

                //Look for colon sign to indicate that previous command has finished
                for (int i = 0; i < mBuffer.Data.Length; ++i)
                {
                    if (mBuffer.Data[i] == ':')
                    {
                        responseFound = true;

                        if (i > 0)
                        {
                            package = new byte[i];
                            Array.Copy(mBuffer.Data, package, i);
                        }
                        break;
                    }
                }

                if (responseFound)
                    mBuffer.Clear();
            }

            if (!responseFound)
                return;

            HandleResponse(package);

            lock (mCommandLock)
                mRunningCommand = null;

            // Try to trigger a new command if any
            RunCommand();
        }
        #endregion

        private void HandleResponse(byte[] responseData)
        {
            string response = null;
            string key = null;
            string value = null;

            if (mRunningCommand == null)
                return;

            if (responseData != null)
            {
                response = System.Text.Encoding.ASCII.GetString(responseData).Replace("\r", "");

                Console.WriteLine("Got response: '{0}'", response);

                if (response.Equals("ERR"))
                {
                    // We're in an ugly loop... only try to get error if we not already tried and failed at it.
                    if (mRunningCommand.Command != Command.GetError)
                    {
                        // Something broke. Queue for it.
                        mCommandQueue.Insert(0, new CommandInfo(Command.GetError));
                    }
                    return;
                }

                if (response.Length > 0)
                {
                    string[] split = response.Split(new char[] { '=' });
                    if (split.Length < 2)
                    {
                        Console.WriteLine("Unknown response: '{0}'", response);
                        return;
                    }

                    key = split[0];
                    value = split[1];
                }
            }

            switch (mRunningCommand.Command)
            {
                case Command.SetPower:
                    State.Power = mRunningCommand.Power;
                    State.WarmingUp = false;

                    // Get source as well when it's powered up
                    if (State.Power)
                        QueueCommand(new CommandInfo(Command.GetSource));
                    break;

                case Command.GetPower:
                    if (key != null && key == "PWR")
                    {
                        State.Power = int.Parse(value) > 0;
                        Console.WriteLine("Power: " + State.Power);

                        // Get source as well when it's powered up
                        if (State.Power)
                            QueueCommand(new CommandInfo(Command.GetSource));
                    }
                    break;

                case Command.GetLampHours:
                    if (key != null && key == "LAMP")
                    {
                        State.LampHours = int.Parse(value);
                        Console.WriteLine("Lamp: " + State.LampHours);
                    }
                    break;

                case Command.GetSource:
                    if (key != null && key == "SOURCE")
                    {
                        if (value == "A0")
                            State.Source = InputSource.Hdmi;
                        else
                            Console.WriteLine("Source " + value + " was not handled");

                        Console.WriteLine("Source: " + State.Source.ToString());
                    }
                    break;

                default:
                    if (response != null)
                        Console.WriteLine("Unhandled response: {0} when running command: {1}", response, mRunningCommand.Command);
                    break;
            }
        }
    }
}
