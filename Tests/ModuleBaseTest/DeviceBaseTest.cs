using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module;
using ModuleBase;

namespace DeviceBaseTest
{
    [TestClass]
    public class DeviceBaseTest
    {
        private DeviceCreationInfo CreateTestInfo()
        {
            dynamic settings = new SettingsObject();
            settings.name = "ProperDeviceName";
            settings.displayName = "ProperDisplayName";

            return new Module.DeviceCreationInfo(settings, new Module.ServiceManager(), new Module.DeviceManager());
        }

        public class TestDevice : DeviceBase
        {
            public TestDevice(DeviceCreationInfo info)
                : base (new DeviceState(), info)
            {
                mState.Archetype = "ProperArchetype";
            }
        }

        private TestDevice CreateTestDevice()
        {
            return new TestDevice(CreateTestInfo());
        }

        [TestMethod]
        public void DeviceBase_TestCopyState()
        {
            var device = CreateTestDevice();

            var copyState = device.CopyState();

            var testState = new DeviceBase.DeviceState();
            testState.Name = "ProperDeviceName";
            testState.DisplayName = "ProperDisplayName";
            testState.Archetype = "ProperArchetype";
            testState.Type = device.GetType().ToString();

            Assert.AreEqual(testState.Name, copyState.Name);
            Assert.AreEqual(testState.DisplayName, copyState.DisplayName);
            Assert.AreEqual(testState.Archetype, copyState.Archetype);
            Assert.AreEqual(testState.Type, copyState.Type);
        }

        [TestMethod]
        public void DeviceBase_TestConstState()
        {
            var device = CreateTestDevice();

            var state = device.CopyState();
            state.Name = "IncorrectName";
            state.DisplayName = "IncorrectDisplayName";
            state.Archetype = "IncorrectArcheType";
            device.ApplyState(state);

            var newState = device.CopyState();

            Assert.AreEqual(state.Name, newState.Name);
            Assert.AreEqual(state.DisplayName, newState.DisplayName);
            Assert.AreEqual(state.Archetype, newState.Archetype);
        }
    }
}
