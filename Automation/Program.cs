using System;
using System.Windows.Forms;
using System.Threading;
using Module;
using Support;
using System.IO;
using System.Collections.Generic;
using Logging;

namespace Automation
{
    class Program : IDisposable
    {
        private volatile bool mRunning = true;

        private ApplicationContext mApplicationContext;
        private Thread mUiThread;

        private ServiceManager mServiceManager;
        private DeviceManager mDeviceManager;
        private Logic mLogic;
        private WebEndpoint.Host mWebHost;
        private UInt16 mPort = 80;

        // Initiated & owned by the UI thread
        private UI.NotifyIcon mNotifyIcon;

        public Program()
        {
            mServiceManager = new ServiceManager();
            mDeviceManager = new DeviceManager();

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Automation";
            Settings settings = new Settings(path, "settings.xml");

            var config = settings.GetConfig();
            if (config.ContainsKey("port"))
                mPort = UInt16.Parse(config["port"]);

            // TODO - Settings shouldn't be responsible to create these. Get device configs and then let DeviceFactory create each instance.
            settings.CreateServices(mServiceManager, mDeviceManager);
            settings.CreateDevices(mServiceManager, mDeviceManager);

            mLogic = new Logic(mDeviceManager, mServiceManager);


            // TODO - Hackish... just make speech part of the system? Or just dispatch to all services once we've created all devices & services
            var speechService = (Speech.SpeechService)mServiceManager.GetService(typeof(Speech.SpeechService));
            if (speechService != null)
            {
                var allCommands = new List<DeviceBase.VoiceCommand>();

                foreach (KeyValuePair<string, DeviceBase> pair in mDeviceManager.Devices)
                {
                    IList<DeviceBase.VoiceCommand> commands = pair.Value.GetVoiceCommands();
                    if (commands != null)
                        allCommands.AddRange(commands);
                }

                speechService.LoadCommands(allCommands);
            }

            // createStatusWindow

            startWebEndpoint();

            StartUIThread();
        }

        public void Dispose()
        {
            mApplicationContext.Dispose();
            mWebHost.Dispose();
            mNotifyIcon.Dispose();
        }

        private void StartUIThread()
        {
            mApplicationContext = new ApplicationContext();
            mUiThread = new Thread(new ThreadStart(RunUI));
        }

        private void JoinUIThread()
        {
            // Signal UI thread to shut down
            mApplicationContext.ExitThread();
            mUiThread.Join();
        }

        [STAThread]
        private void RunUI()
        {
            // Init stuff

            // Need to do this from the same thread as Application.Run() will run from...
            PortNotifier.Init();

            mNotifyIcon = new UI.NotifyIcon(Properties.Resources.NotifyIcon);

            // FIXME - NOT thread safe
            updateNotificationStatus();

            Application.Run(mApplicationContext);

            // Signal quit
            mRunning = false;
        }

        public void Run()
        {
            mUiThread.Start();
            //SerialHelper helper = new SerialHelper("rfxcom", 3, 38400);

            while (mRunning)
            {
                // Check for updated .dll files.

                // Delay slightly to prevent multiple updates during a copy

                // Ensure no requests are currently active.

                //stopWebEndpoint();

                // Recreate updated services

                //startWebEndpoint();


                // FIXME 
                //EventWaitHandle
                Thread.Sleep(1000);

            }

            JoinUIThread();
        }

        private void updateNotificationStatus()
        {
            string services = "";
            foreach (var service in mServiceManager.Services)
                services += "\n" + service.Name;

            mNotifyIcon.SetStatus(mServiceManager.Services.Capacity + " services running:" + services);
        }

        public void onServicesUpdated()
        {
            updateNotificationStatus();
        }

        public void startWebEndpoint()
        {
            mWebHost = new WebEndpoint.Host(mPort, mServiceManager, mDeviceManager);
        }

        private void stopWebEndpoint()
        {
            mWebHost.Stop();
            mWebHost = null;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
//        [STAThread]
        static void Main()
        {
            try
            {
                using (Program program = new Program())
                {
                    program.Run();
                }
            }
            catch (Exception e)
            {
                string message = string.Format("Unhandled exception: {0}\n\nLocation: {1}\n\nCallstack:\n{2}\n\nInner: {3}\n\nCallstack:\n{4}", e.Message, e.TargetSite, e.StackTrace, e.InnerException != null ? e.InnerException.Message : "<no inner exception>", e.InnerException != null ? e.InnerException.StackTrace : "<no inner exception>");

                MessageBox.Show(message, "Exception");

                Log.Fatal(message);
                var inner = e.InnerException;
                while (inner != null)
                {
                    string innerMessage = string.Format("Inner exception: {0}\n\nLocation: {1}\n\nCallstack:\n{2}\n\n", inner.Message, inner.TargetSite, inner.StackTrace);
                    Console.WriteLine(innerMessage);
                    inner = inner.InnerException;
                }

                throw;
            }
        }
    }
}
