using ArduinoLamps;
using Bluetooth;
using ComputerProxy;
using Curtain;
using Epson;
using Events;
using Logging;
using Mode;
using Module;
using RfxCom;
using Scene;
using Speech;
using Support;
using System;
using System.Collections.Generic;
using System.Threading;
using WakeOnLan;
using Yamaha;

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

        private const int MODE_AWAY_TIMEOUT = 30;

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

        private ModeService.Mode mModeAway = new ModeService.Mode("away");
        private ModeService.Mode mModeNormal = new ModeService.Mode("normal");
        private ModeService.Mode mModeNight = new ModeService.Mode("night");
        private ModeService.Mode mModeOff = new ModeService.Mode("off");

        // Livingroom devices
        private CurtainDevice mCurtainLivingroom;
        private EpsonDevice mProjector;
        private YamahaDevice mReceiver;
        private NexaLampDevice mLampLivingroom;
        private NexaLampDevice mLampTable;
        private NexaLampDevice mLampWindow;
        private LampDevice mLampDarthLeft;
        private LampDevice mLampDarthRight;
        private NexaSensorDevice mMotionLivingroom;
        private NexaSensorDevice mSwitchSofa1;
        private NexaSensorDevice mSwitchSofa2;
        private ComputerProxyDevice mDesktop;

        // Bedroom devices
        private CurtainDevice mCurtainBedroom;
        private NexaLampDevice mLampBedroom;
        private NexaLampDevice mLampBed;

        // Hallway devices
        private NexaLampDevice mLampHallway;
        private NexaSensorDevice mMotionDoor;
        private NexaSensorDevice mSwitchHallway;

        // Presence devices
        private BluetoothDevice mNexus5;

        public Logic(DeviceManager deviceManager, ServiceManager serviceManager)
        {
            mDeviceManager = deviceManager;
            mServiceManager = serviceManager;

            mWake = GetService<WakeOnLanService>();

            mMode = GetService<ModeService>();

            if (mMode != null)
                mMode.OnModeChange += OnModeChange;

            mScene = GetService<SceneService>();
            if (mScene != null)
                mScene.OnSceneEvent += OnSceneEvent;

            mEvents = GetService<EventService>();
            if (mEvents != null)
                mEvents.OnEvent += OnExternalEvent;

            // Livingroom
            mCurtainLivingroom = (CurtainDevice)deviceManager.GetDevice("curtain_livingroom");
            mProjector = (EpsonDevice)deviceManager.GetDevice("projector");
            mReceiver = (YamahaDevice)deviceManager.GetDevice("receiver");
            mLampLivingroom = (NexaLampDevice)deviceManager.GetDevice("lamp_livingroom");
            mLampTable = (NexaLampDevice)deviceManager.GetDevice("lamp_table");
            mLampWindow = (NexaLampDevice)deviceManager.GetDevice("lamp_window");
            mLampDarthLeft = (ArduinoLamps.LampDevice)deviceManager.GetDevice("lamp_darth_left");
            mLampDarthRight = (ArduinoLamps.LampDevice)deviceManager.GetDevice("lamp_darth_right");
            mMotionLivingroom = (NexaSensorDevice)deviceManager.GetDevice("motion_livingroom");

            mSwitchSofa1 = (NexaSensorDevice)deviceManager.GetDevice("switch_sofa1");
            mSwitchSofa2 = (NexaSensorDevice)deviceManager.GetDevice("switch_sofa2");
            if (mSwitchSofa1 != null)
                mSwitchSofa1.OnDeviceEvent += OnSofaSwitchEvent;
            if (mSwitchSofa2 != null)
                mSwitchSofa2.OnDeviceEvent += OnSofaSwitchEvent;

            mDesktop = (ComputerProxyDevice)deviceManager.GetDevice("desktop");

            // Bedroom
            mCurtainBedroom = (CurtainDevice)deviceManager.GetDevice("curtain_bedroom");
            mLampBedroom = (NexaLampDevice)deviceManager.GetDevice("lamp_bedroom");
            mLampBed = (NexaLampDevice)deviceManager.GetDevice("lamp_bed");

            // Hallway
            mLampHallway = (NexaLampDevice)deviceManager.GetDevice("lamp_hallway");
            mMotionDoor = (NexaSensorDevice)deviceManager.GetDevice("motion_door");
            if (mMotionDoor != null)
                mMotionDoor.OnDeviceEvent += OnDoorSensor;

            mSwitchHallway = (NexaSensorDevice)deviceManager.GetDevice("switch_hallway");
            if (mSwitchHallway != null)
                mSwitchHallway.OnDeviceEvent += OnHallwaySwitch;

            // Presence devices
            mNexus5 = (BluetoothDevice)deviceManager.GetDevice("presence_nexus_5");
            if (mNexus5 != null)
                mNexus5.OnDeviceEvent += OnBluetoothEvent;

            mSpeech = GetService<SpeechService>();
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
            Log.Info("Applying light cinema");
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
            Log.Info("Applying misc cinema");
            mCurtainLivingroom.Down();

            mDesktop.SetMonitorPower(false);
            mLampTable.SwitchDevice(false);
            mLampWindow.SwitchDevice(false);
        }

        private void ApplyCinemaOffLight()
        {
            Log.Info("Canceling light cinema mode");
            mReceiver.SetPower(false);
            if (mProjector != null)
                mProjector.PowerOff();
        }

        private void ApplyCinemaOffMisc()
        {
            Log.Info("Canceling misc cinema");
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
            mLampWindow.SwitchDevice(false);
            mLampDarthLeft.SwitchDevice(false);
            mLampDarthRight.SwitchDevice(false);
            mLampBedroom.SwitchDevice(false);
            mLampBed.SwitchDevice(false);
            mLampHallway.SwitchDevice(false);
        }

        private void MacroLeaving()
        {
            if (!TimeHelper.IsDay)
                mLampHallway.SwitchDevice(true);

            mDesktop.SetPower(false);
            mReceiver.SetPower(false);
            if (mProjector != null)
                mProjector.PowerOff();

            mLampLivingroom.SwitchDevice(false);
            mLampTable.SwitchDevice(false);
            mLampWindow.SwitchDevice(false);
            mLampBedroom.SwitchDevice(false);
            mLampBed.SwitchDevice(false);
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
        }
        #endregion

        private void OnSofaSwitchEvent(object sender, NexaEvent nexaEvent)
        {
            // Sofa table switches
            if (nexaEvent.Device == mSwitchSofa1)
            {
                Log.Debug("Sofa switch 1");

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
                Log.Debug("Sofa switch 2");
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
       }

        private void OnDoorSensor(object sender, NexaEvent nexaEvent)
        {
            Log.Debug("Door sensor");

            // On
            if (nexaEvent.Value)
            {
                if (mMode != null && mMode.CurrentMode == mModeAway)
                {
                    if (!TimeHelper.IsDay)
                        mLampHallway.SwitchDevice(true);

                    if (mNexus5 != null)
                        mNexus5.CheckDevice();      // Trigger scan
                }
            }
        }

        private void OnHallwaySwitch(object sender, NexaEvent nexaEvent)
        {
            Log.Debug("Hallway switch");

            if (nexaEvent.Value)
            {
                // Up position.

                // Cancel pending changes so we don't reapply away in case down and then up was pressed.
                if (mMode != null)
                    mMode.CancelPendingChanges();
                HomeScene();
            }
            else
            {
                // Down position.

                if (mMode != null)
                {
                    if (mMode.CurrentMode != mModeAway)
                    {
                        Speak(string.Format("Goodbye, activating away mode in {0} seconds", MODE_AWAY_TIMEOUT));

                        RunMacro("leaving");
                        mMode.QueueChange(mModeAway, MODE_AWAY_TIMEOUT);
                    }
                    else
                    {
                        Speak("Already away");
                    }
                }
            }
        }

        private void OnModeChange(object sender, ModeService.ModeEvent modeEvent)
        {
            if (modeEvent.NewMode == mModeNormal)
            {
                Speak("Activating normal mode");
                RunMacro("home");
            }
            else if (modeEvent.NewMode == mModeNight)
            {
                Speak("Activating night mode. Sleep tight.");
                RunMacro("night");
            }
            else if (modeEvent.NewMode == mModeAway)
            {
                Log.Info("Away mode activated");
                RunMacro("off");
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

                bool isAway = currentMode == mModeAway || currentMode == mModeOff;
                if (!isAway)
                    return;

                Speak("Welcome home");

                TimeOfDay tod = TimeHelper.TimeOfDay;
                if (tod != TimeOfDay.Night)
                    mMode.CurrentMode = mModeNormal;
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
                    mMode.CurrentMode = mModeAway;
            }
        }

        private T GetService<T>() where T : ServiceBase
        {
            var service = (T)mServiceManager.GetService(typeof(T));
            if (service == null)
                Log.Warning("Missing service: " + typeof(T).ToString());
            return service;
        }
    }
}
