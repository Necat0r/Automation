using Module;

namespace Epson
{
    public class EpsonService : ServiceBase
    {
        public const string SOURCE_UNKNOWN = "<unknown>";
        public const string SOURCE_HDMI = "A0";

        public const int STATUS_OFF = 0;
        public const int STATUS_PENDING = 1;
        public const int STATUS_ON = 2;

        public const string KEY_ENTER = "KEY 01";
        public const string KEY_MENU = "KEY 03";
        public const string KEY_UP = "KEY 35";
        public const string KEY_DOWN = "KEY 36";
        public const string KEY_LEFT = "KEY 37";
        public const string KEY_RIGHT = "KEY 38";

        //private SerialHelper mSerialHelper;
        //private object mLock = new object();

        public EpsonService(string name, ServiceCreationInfo info)
            : base("epson", info)
        {
            //mSerialHelper = new SerialHelper("epson", (uint)(port - 1), 9600);
        }

/*
        // Get current power status
        [ServiceGetContract("power")]
        public bool OnPowerRequest()
        {
            // /projector/power

            return IsPoweredOn();
        }

        [ServicePutContract("power")]
        public void OnPowerRequest(bool value)
        {
            // /projector/power?value=on
            // /projector/power?value=off

            if (value)
                PowerOn();
            else
                PowerOff();
        }

        [ServicePutContract("source")]
        public bool OnSourceRequest(string source = SOURCE_HDMI)
        {
            // /projector/source?value=hdmi

            if (source.Equals(source == "hdmi"))
            {
                SelectSource(SOURCE_HDMI);
                return true; //, "Success";
            }
            return false;
        }

        [ServicePutContract("key")]
        public bool OnKeyRequest(string key = null)
        {
            // /projector/key?value=up

            return true;
        }

        public bool RunCommand(string command, int timeout = 5)
        {
            lock(mLock)
            {
                byte[] data = command + "\r";
                mSerialHelper.WriteData(data);
                string result;
                while (timeout > 0)
                {
                    // Append to result until we"ve read everything.
                    result = result + serial.read(20);

                    // If we"ve received the ":" sign, the command has finished
                    if (result.IndexOf(":") >= 0)
                        break;

                    Thread.Sleep(1);
                    timeout -= 1;
                }

            }

            if (timeout == 0)
            {
                Console.WriteLine("Command " + command + " timed out");
                return false, "TIMEOUT";
            }

            // Clean up result
            result = result.rstrip(b"\r:");

            if (result.find(b"ERR") >= 0)
                return false, result;

            return true; //, result.decode("utf-8")
        }

        public bool RunStandardCommand(string command)
        {
            bool ready = IsPoweredOn();
            if (!ready)
                return false;

            return RunCommand(command);
        }

        public bool IsPoweredOn()
        {
            bool success, result = RunCommand("PWR?");

            dummy = result.split("=");
            if (len(dummy) == 2)
                return bool(int(dummy[1]) > 0);

            return false;
        }

        public bool PowerOn()
        {
            bool ready = IsPoweredOn();
            if (ready)
                return true;

            return RunCommand("PWR ON", 40);
        }

        public bool PowerOff()
        {
            bool ready = IsPoweredOn();
            if (!ready)
                return true;

            return  RunCommand("PWR OFF", 50);
        }

        public bool GetSource()
        {
            bool success, result = RunStandardCommand("SOURCE?");

            if (!success)
                return false, SOURCE_UNKNOWN;

            dummy = result.split("=");
            if (len(dummy) == 2)
            {
                if (dummy[1] == SOURCE_HDMI)
                    return true, "hdmi";
            }

            return true, SOURCE_UNKNOWN;
        }

        public bool SelectSource(string source)
        {
            return RunStandardCommand("SOURCE " + source);
        }

        public bool SendKey(string key)
        {
            return RunCommand(key, 1);
        }
 */
    }
}
