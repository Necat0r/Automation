using ArduinoLamps;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module;
using ModuleBaseTest;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ModulesTest
{
    [TestClass]
    public class ArduinoLampsTest
    {
        private class LampServiceMock : LampServiceBase
        {
            public LampServiceMock(string name, ServiceCreationInfo info)
                : base("LampTest", info)
            {
                SetLevelCalled = false;
                Level = 0.0f;
            }

            public override void SetLevel(LampDevice lampDevice, float level)
            {
                SetLevelCalled = true;
                Level = level;
            }

            public static bool SetLevelCalled { get; set; }
            public static float Level { get; set; }
        }

        private LampDevice CreateLampDevice()
        {
            var util = new TestUtil();

            string name = "LampTest";
            string type = typeof(LampServiceMock).AssemblyQualifiedName;

            var serviceConfiguration = new Dictionary<string, string>();
            util.CreateService(name, type, serviceConfiguration);

            var deviceConfiguration = GetDeviceConfiguration();

            return (LampDevice)util.CreateDevice(deviceConfiguration);
        }

        private dynamic GetDeviceConfiguration()
        {
            dynamic config = TestUtil.GetDefaultDeviceConfig(typeof(LampDevice));
            config.channel = "0";

            return config;
        }

        [TestMethod]
        public void ArduinoLamps_TestDeviceCreation()
        {
            var device = CreateLampDevice();
            Assert.IsNotNull(device);

            Assert.AreEqual(0, device.Channel);
        }

        [TestMethod]
        [ExpectedException(typeof(TargetInvocationException))]
        public void ArduinoLamps_TestMissingService()
        {
            var util = new TestUtil();
            try
            {
                var device = util.CreateDevice(GetDeviceConfiguration());
            }
            catch (TargetInvocationException e)
            {
                Assert.AreEqual(typeof(InvalidOperationException), e.InnerException.GetType());
                throw;
            }
        }

        [TestMethod]
        public void ArduinoLamps_TestDeviceConstState()
        {
            var device = CreateLampDevice();

            var state = (LampDevice.LampState)device.CopyState();
            state.Channel = 0;
            device.ApplyState(state);
            var newState = (LampDevice.LampState)device.CopyState();

            Assert.AreEqual(state.Channel, newState.Channel);
        }

        [TestMethod]
        public void ArduinoLamps_TestDeviceSwitching()
        {
            var device = CreateLampDevice();

            device.SwitchDevice(true);
            Assert.IsTrue(LampServiceMock.SetLevelCalled);
            Assert.AreEqual(1.0f, LampServiceMock.Level);

            device.SwitchDevice(false);
            Assert.AreEqual(0.0f, LampServiceMock.Level);
        }

        [TestMethod]
        public void ArduinoLamps_TestDeviceDimming()
        {
            var device = CreateLampDevice();

            device.DimDevice(0.5f);
            Assert.IsTrue(LampServiceMock.SetLevelCalled);
            Assert.AreEqual(0.5f, LampServiceMock.Level);
        }

        [TestMethod]
        public void ArduinoLamps_TestLevelClamping()
        {
            var device = CreateLampDevice();

            device.DimDevice(-1.0f);
            Assert.IsTrue(LampServiceMock.SetLevelCalled);
            Assert.AreEqual(0.0f, LampServiceMock.Level);

            device.DimDevice(2.0f);
            Assert.AreEqual(1.0f, LampServiceMock.Level);
        }

    }
}
