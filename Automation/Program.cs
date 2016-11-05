using System;
using System.Windows.Forms;
using System.Threading;
using Module;
using Support;
using System.IO;
using System.Collections.Generic;
using Logging;
using ModuleBase;

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

            // Create services
            var serviceConfigs = settings.GetServiceConfigs();
            CreateServices(serviceConfigs);

            // Create devices
            var deviceConfigs = settings.GetDeviceConfigs();
            CreateDevices(deviceConfigs);

            mLogic = new Logic(mDeviceManager, mServiceManager);


            InitSpeechCommands();

            // createStatusWindow

            startWebEndpoint();

            StartUIThread();
        }

        private void CreateServices(List<dynamic> configs)
        {
            foreach (var serviceConfig in configs)
            {
                ServiceBase service;
                try
                {
                    ServiceCreationInfo info = new ServiceCreationInfo(serviceConfig, mServiceManager, mDeviceManager);

                    service = ServiceFactory.CreateService(info);
                    mServiceManager.AddService(service);
                }
                catch (Exception e)
                {
                    Log.Error("Failed creating service for node: " + serviceConfig.Name);
                    if (e.InnerException != null)
                        Log.Error("Inner Exception: {0}\nCallstack:\n{1}", e.InnerException.Message, e.InnerException.StackTrace);
                    else
                        Log.Error("Exception: {0}\nCallstack:\n{1}", e.Message, e.StackTrace);

                    continue;
                }

                Log.Info("Created service: {0} of type: {1}", service.Name, service.GetType().ToString());
            }
        }

        private void CreateDevices(List<dynamic> configs)
        {
            foreach (var deviceConfig in configs)
            {
                DeviceBase device;
                try
                {
                    DeviceCreationInfo info = new DeviceCreationInfo(deviceConfig, mServiceManager, mDeviceManager);
                    device = DeviceFactory.CreateDevice(info);
                    mDeviceManager.AddDevice(device);
                }
                catch (Exception e)
                {
                    Log.Error("Failed creating device for node with config: " + deviceConfig.name);
                    if (e.InnerException != null)
                        Log.Error("Inner Exception: {0}\nCallstack:\n{1}", e.InnerException.Message, e.InnerException.StackTrace);
                    else
                        Log.Error("Exception: {0}\nCallstack:\n{1}", e.Message, e.StackTrace);

                    continue;
                }

                Log.Info("Created device: {0} of type: {1}", device.Name, device.GetType().ToString());
            }
        }

        private void InitSpeechCommands()
        {
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mServiceManager != null)
                    mServiceManager.Dispose();
                mServiceManager = null;

                if (mApplicationContext != null)
                    mApplicationContext.Dispose();
                mApplicationContext = null;

                if (mWebHost != null)
                    mWebHost.Dispose();
                mWebHost = null;

                if (mNotifyIcon != null)
                    mNotifyIcon.Dispose();
                mNotifyIcon = null;
            }
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
            try
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
            catch (Exception e)
            {
                Logging.Log.Exception(e);
            }
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
                    Log.Error(innerMessage);
                    inner = inner.InnerException;
                }

                throw;
            }
        }
    }
}
