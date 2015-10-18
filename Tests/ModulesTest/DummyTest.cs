using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModuleBaseTest;
using Dummy;

namespace ModulesTest
{
    /// <summary>
    /// Summary description for DummyTest
    /// </summary>
    [TestClass]
    public class DummyTest
    {
        private DummyDevice CreateDummyDevice()
        {
            var util = new TestUtil();

            dynamic config = TestUtil.GetDefaultDeviceConfig(typeof(DummyDevice));

            return (DummyDevice)util.CreateDevice(config);
        }

        [TestMethod]
        public void Dummy_TestDeviceCreation()
        {
            var device = CreateDummyDevice();
            Assert.IsNotNull(device);
        }
    }
}
