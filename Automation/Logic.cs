using System;
using RfxComService;
using Curtain;
using Epson;
using Speech;
using WakeOnLan;
using Bluetooth;
using Yamaha;
using System.Threading;
using Mode;
using ComputerProxy;
using Module;
using Scene;
using Support;
using System.Collections.Generic;
using ArduinoLamps;
using Events;

namespace Automation
{
    class Logic
    {
        // TODO - Fix IsDay
        //s = sun(latitude=59.3294, longitude=18.0686)

        private DeviceManager mDeviceManager;
        private ServiceManager mServiceManager;

        private const int RECEIVER_SCENE_HTPC = 0;
        private const int RECEIVER_SCENE_DESKTOP = 2;

        private enum SofaSwitch
        {
            None,
            Button0On,
            Button0Off,
            Button1On,
            Button1Off
        }
        private SofaSwitch mSofaSwitch = SofaSwitch.None;

        // Services
        private ModeService mMode;
        private SceneService mScene;
        private WakeOnLanService mWake;
        private SpeechService mSpeech;
        private EventService mEvents;

        // Livingroom devices
        private CurtainDevice mCurtainLivingroom;
        private EpsonDevice mProjector;
        private YamahaDevice mReceiver;
        private NexaDevice mLampLivingroom;
        private NexaDevice mLampTable;
        private NexaDevice mLampTv;
        private NexaDevice mLampWindow;
        private LampDevice mLampDarthLeft;
        private LampDevice mLampDarthRight;
        private NexaDevice mMotionLivingroom;
        private NexaDevice mSwitchSofa1;
        private NexaDevice mSwitchSofa2;
        private ComputerProxyDevice mDesktop;

        // Bedroom devices
        private CurtainDevice mCurtainBedroom;
        private NexaDevice mLampBedroom;
        private NexaDevice mLampBed;

        // Hallway devices
        private NexaDevice mLampHallway;
        private NexaDevice mMotionDoor;

        // Presence devices
        private BluetoothDevice mNexus5;

        public Logic(DeviceManager deviceManager, ServiceManager serviceManager)
        {
            mDeviceManager = deviceManager;
            mServiceManager = serviceManager;

            RfxComService.RfxComService rfxCom = (RfxComService.RfxComService)serviceManager.GetService(typeof(RfxComService.RfxComService));
            if (rfxCom != null)
                rfxCom.OnNexaEvent += OnNexaEvent;

            mWake = (WakeOnLanService)serviceManager.GetService(typeof(WakeOnLan.WakeOnLanService));

            mMode = (ModeService)serviceManager.GetService(typeof(ModeService));
            if (mMode != null)
                mMode.OnModeChange += OnModeChange;

            mScene = (SceneService)serviceManager.GetService(typeof(SceneService));
            if (mScene != null)
                mScene.OnSceneEvent += OnSceneEvent;

            mEvents = (EventService)serviceManager.GetService(typeof(EventService));
            if (mEvents != null)
                mEvents.OnEvent += OnExternalEvent;

            // Livingroom
            mCurtainLivingroom = (CurtainDevice)deviceManager.GetDevice("curtain_livingroom");
            mProjector = (EpsonDevice)deviceManager.GetDevice("projector");
            mReceiver = (YamahaDevice)deviceManager.GetDevice("receiver");
            mLampLivingroom = (NexaDevice)deviceManager.GetDevice("lamp_livingroom");
            mLampTable = (NexaDevice)deviceManager.GetDevice("lamp_table");
            mLampTv = (NexaDevice)deviceManager.GetDevice("lamp_tv");
            mLampWindow = (NexaDevice)deviceManager.GetDevice("lamp_window");
            mLampDarthLeft = (ArduinoLamps.LampDevice)deviceManager.GetDevice("lamp_darth_left");
            mLampDarthRight = (ArduinoLamps.LampDevice)deviceManager.GetDevice("lamp_darth_right");
            mMotionLivingroom = (NexaDevice)deviceManager.GetDevice("motion_livingroom");
            mSwitchSofa1 = (NexaDevice)deviceManager.GetDevice("switch_sofa1");
            mSwitchSofa2 = (NexaDevice)deviceManager.GetDevice("switch_sofa2");
            mDesktop = (ComputerProxyDevice)deviceManager.GetDevice("desktop");

            // Bedroom
            mCurtainBedroom = (CurtainDevice)deviceManager.GetDevice("curtain_bedroom");
            mLampBedroom = (NexaDevice)deviceManager.GetDevice("lamp_bedroom");
            mLampBed = (NexaDevice)deviceManager.GetDevice("lamp_bed");

            // Hallway
            mLampHallway = (NexaDevice)deviceManager.GetDevice("lamp_hallway");
            mMotionDoor = (NexaDevice)deviceManager.GetDevice("motion_door");

            // Presence devices
            mNexus5 = (BluetoothDevice)deviceManager.GetDevice("presence_nexus_5");
            if (mNexus5 != null)
                mNexus5.OnDeviceEvent += OnBluetoothEvent;

            mSpeech = (SpeechService)serviceManager.GetService(typeof(Speech.SpeechService));
            if (mSpeech != null)
            {
                var commands = new List<DeviceBase.VoiceCommand>();
                commands.Add(new DeviceBase.VoiceCommand("activate night mode", () => { MacroNight(); }));

                mSpeech.LoadCommands(commands);
            }

            Speak("System ready");
        }

        private void Speak(string message)
        {
            if (mSpeech != null)
                mSpeech.Speak(message);
        }


        #region Scenes
        private void ApplyCinemaLight()
        {
            Console.WriteLine("Applying light cinema");
            if (mReceiver != null)
            {
                mReceiver.SetPower(true);
                mReceiver.SetScene(RECEIVER_SCENE_HTPC);
            }

            mLampLivingroom.SwitchDevice(false);

            if (mProjector != null)
                mProjector.PowerOn();
        }

        private void ApplyCinemaMisc()
        {
            Console.WriteLine("Applying misc cinema");
            mCurtainLivingroom.Down();

            mDesktop.SetMonitorPower(false);
            mLampTable.SwitchDevice(false);
            mLampWindow.SwitchDevice(false);
            mLampTv.SwitchDevice(true);
        }

        private void ApplyCinemaOffLight()
        {
            Console.WriteLine("Canceling light cinema mode");
            mReceiver.SetPower(false);
            if (mProjector != null)
                mProjector.PowerOff();
        }

        private void ApplyCinemaOffMisc()
        {
            Console.WriteLine("Canceling misc cinema");
            mLampTv.SwitchDevice(false);
            mCurtainLivingroom.Up();
        }

        // Turn everything off
        private void MacroOff()
        {
            mDesktop.SetPower(false);
            mReceiver.SetPower(false);
            if (mProjector != null)
                mProjector.PowerOff();

            mLampLivingroom.SwitchDevice(false);
            mLampTable.SwitchDevice(false);
            mLampTv.SwitchDevice(false);
            mLampWindow.SwitchDevice(false);
            mLampDarthLeft.SwitchDevice(false);
            mLampDarthRight.SwitchDevice(false);
            mLampBedroom.SwitchDevice(false);
            mLampBed.SwitchDevice(false);
            mLampHallway.SwitchDevice(false);
        }

        private void MacroLeaving()
        {
            mDesktop.SetPower(false);;
            mReceiver.SetPower(false);
            if (mProjector != null)
                mProjector.PowerOff();

            mLampLivingroom.SwitchDevice(false);
            mLampTable.SwitchDevice(false);
            mLampTv.SwitchDevice(false);
            mLampWindow.SwitchDevice(false);
            mLampBedroom.SwitchDevice(false);
            mLampBed.SwitchDevice(false);

            if (!TimeHelper.IsDay)
                mLampHallway.SwitchDevice(true);
        }

        private void MacroHome()
        {
            TimeOfDay tod = TimeHelper.TimeOfDay;

            if (tod == TimeOfDay.Evening || tod == TimeOfDay.Day)
                mDesktop.SetPower(true);

            if (tod == TimeOfDay.Evening || tod == TimeOfDay.Night)
            {
                mLampHallway.SwitchDevice(true);

                if (tod != TimeOfDay.Night)
                {
                    mLampTable.SwitchDevice(true);
                    mLampBedroom.SwitchDevice(true);
                    mLampDarthLeft.SwitchDevice(true);
                    mLampDarthRight.SwitchDevice(true);
                }
                else
                {
                    mLampBed.SwitchDevice(true);
                }
            }
        }

        private void MacroNight()
        {
            mDesktop.SetPower(false);

            mLampTable.SwitchDevice(false);
            mLampBed.SwitchDevice(true);
            mCurtainBedroom.Down();

            mLampBedroom.SwitchDevice(false);
            mLampLivingroom.SwitchDevice(false);
            mLampTv.SwitchDevice(false);
            mLampWindow.SwitchDevice(false);
            mLampHallway.SwitchDevice(false);
            mLampDarthLeft.SwitchDevice(false);
            mLampDarthRight.SwitchDevice(false);

            mReceiver.SetPower(false);
            if (mProjector != null)
                mProjector.PowerOff();
        }

        private void MacroCinema()
        {
            Speak("Applying cinema mode");
            ApplyCinemaLight();
            ApplyCinemaMisc();
        }

        private void MacroCinemaOff()
        {
            Speak("Canceling cinema mode");
            ApplyCinemaOffLight();
            ApplyCinemaOffMisc();
        }

        private void MacroVideo()
        {
            Speak("Applying video mode");
            ApplyCinemaLight();
        }

        private void MacroVideoComputer()
        {
            Speak("Applying video computer mode");
            if (mReceiver != null)
            {
                mReceiver.SetPower(true);
                mReceiver.SetScene(RECEIVER_SCENE_DESKTOP);
            }

            if (mProjector != null)
                mProjector.PowerOn();
        }

        private void MacroVideoOff()
        {
            Speak("Canceling video mode");
            ApplyCinemaOffLight();
        }

        private void MacroCurtainTest()
        {
            // 200ms / send.
            var down = false;
            if (down)
            {
            // Top -> bottom 17.95  (200ms locked)

            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_UP)
            //            time.sleep(5)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_DOWN)
            //            time.sleep(17.95)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_STOP)

                mCurtainLivingroom.Up();
                Thread.Sleep(10000);
                mCurtainLivingroom.Down();
                Thread.Sleep(5000);
                mCurtainLivingroom.Stop();
            //            time.sleep(3)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_DOWN)
            //            time.sleep(11)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_STOP)
            }
            else
            {
            // Bottom -> top 18.25   (200ms locked)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_DOWN)
            //            time.sleep(5)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_UP)
            //            time.sleep(18.25)
            //            self.curtain.sendKey(self.CURTAIN_LIVINGROOM, CurtainService.KEY_STOP)

                mCurtainLivingroom.Down();
                Thread.Sleep(5000);
                mCurtainLivingroom.Up();
                Thread.Sleep(5000);
                mCurtainLivingroom.Stop();
                Thread.Sleep(3000);
                mCurtainLivingroom.Up();
                Thread.Sleep(11000);
                mCurtainLivingroom.Stop();
            }
        }

        private void MacroTest()
        {
            mDesktop.SetMonitorPower(false);
        }

        private bool RunMacro(string macroName)
        {
            macroName = macroName.ToLower();
            switch (macroName)
            {
                case "off":             MacroOff(); break;
                case "leaving":         MacroLeaving(); break;
                case "home":            MacroHome(); break;
                case "night":           MacroNight(); break;
                case "cinema":          MacroCinema(); break;
                case "cinema_off":      MacroCinemaOff(); break;
                case "video":           MacroVideo(); break;
                case "video_computer":  MacroVideoComputer(); break;
                case "video_off":       MacroVideoOff(); break;
                case "curtaintest":     MacroCurtainTest(); break;
                case "test":            MacroTest(); break;
            }

            return true;

            //var mapping = new Dictionary<string, Delegate>();
            //var mapping = {self.MACRO_CINEMA: self.macroCinema,
            //           self.MACRO_CINEMAOff: self.macroCinemaOff,
            //           "off": self.macroOff,
            //           "leaving": self.macroLeaving,
            //           "home": self.macroHome,
            //           "night": self.macroNight,
            //           "cinema": self.macroCinema,
            //           "cinema_off": self.macroCinemaOff,
            //           "video": self.macroVideo,
            //           "video_computer": self.macroVideoComputer,
            //           "video_off": self.macroVideoOff,
            //           "curtaintest": self.macroCurtainTest,
            //           "test": self.macroTest}

            //macroName = macroName.ToLower();
            //if (macroName in mapping)
            //{
            //    Console.WriteLine("Running macro", macroName);
            //    mapping[macroName]();
            //    return true;
            //}
            //return false;
        }
        #endregion

        private void OnNexaEvent(object sender, NexaEvent nexaEvent)
        {
            // Sofa table switches
            if (nexaEvent.Device == mSwitchSofa1)
            {
                Console.WriteLine("Sofa switch 1");

                if (nexaEvent.Value)
                {
                    if (mSofaSwitch != SofaSwitch.Button0On)
                        // 1st action
                        ApplyCinemaLight();
                    else
                        // 2nd action
                        ApplyCinemaMisc();
                    mSofaSwitch = SofaSwitch.Button0On;
                }
                else
                {
                    if (mSofaSwitch != SofaSwitch.Button0Off)
                        // 1st action
                        ApplyCinemaOffLight();
                    else
                        // 2nd action
                        ApplyCinemaOffMisc();
                    mSofaSwitch = SofaSwitch.Button0Off;
                }
            }
            else if (nexaEvent.Device == mSwitchSofa2)
            {
                Console.WriteLine("Sofa switch 2");
                if (nexaEvent.Value)
                {
                    if (mSofaSwitch != SofaSwitch.Button1On)
                    {
                        // 1st action
                        mReceiver.SetPower(true);
                        mReceiver.SetScene(RECEIVER_SCENE_HTPC);
                    }
                    else
                    {
                        // 2nd action
                        if (mProjector != null)
                            mProjector.PowerOn();
                    }
                    mSofaSwitch = SofaSwitch.Button1On;
                }
                else
                {
                    mReceiver.SetPower(false);
                    if (mProjector != null)
                        mProjector.PowerOff();
                    mSofaSwitch = SofaSwitch.Button1Off;
                }
            }

            // Motion sensor
            if (nexaEvent.Device == mMotionDoor)
            {
                Console.WriteLine("Door sensor");

                // On
                if (nexaEvent.Value)
                {
                    if (mMode != null && mMode.CurrentMode == ModeService.Mode.Away)
                    {
                        if (!IsDay())
                            mLampHallway.SwitchDevice(true);

                        if (mNexus5 != null)
                            mNexus5.CheckDevice();      // Trigger scan
                    }
                }
            }
        }

        private void OnModeChange(object sender, ModeService.ModeEvent modeEvent)
        {
            // TODO - Enable once pending is re-implemented
            //if (modeEvent.pending)
            //{
            //    if (modeEvent.NewMode == ModeService.Mode.Away)
            //        // We"re about to leave shut everything off except hallway
            //        RunMacro("leaving");
            //}
            //else
            {
                if (modeEvent.NewMode == ModeService.Mode.Normal)
                {
                    Speak("Activating normal mode");
                    RunMacro("home");
                }
                else if (modeEvent.NewMode == ModeService.Mode.Night)
                {
                    Speak("Activating night mode. Sleep tight.");
                    RunMacro("night");
                }
                else if (modeEvent.NewMode == ModeService.Mode.Away)
                {
                    Console.WriteLine("Away mode activated");
                    RunMacro("off");
                }
            }
        }

        private void OnSceneEvent(object sender, SceneEvent e)
        {
            RunMacro(e.Scene);
        }

        private void OnExternalEvent(object sender, Events.EventService.Event externalEvent)
        {
            if (externalEvent.Name == "phoneInRange")
            {
                string id;
                bool result = externalEvent.Data.TryGetValue("Id", out id);
                if (result)
                    HomeScene();
            }
        }

        private void HomeScene()
        {
            if (mMode != null)
            {
                var currentMode = mMode.CurrentMode;

                bool isAway = currentMode == ModeService.Mode.Away || currentMode == ModeService.Mode.Off;
                if (!isAway)
                    return;

                Speak("Welcome home, Jonas");

                TimeOfDay tod = TimeHelper.TimeOfDay;
                if (tod != TimeOfDay.Night)
                    mMode.CurrentMode = ModeService.Mode.Normal;
                //else
                //    mMode.CurrentMode = ModeService.Mode.Night;
            }
        }

        private void OnBluetoothEvent(object sender, BluetoothDeviceEvent e)
        {
            if (e.InRange)
                HomeScene();
            else
            {
                // Lost device
                if (mMode != null)
                    mMode.CurrentMode = ModeService.Mode.Away;
            }
        }
    }
}
