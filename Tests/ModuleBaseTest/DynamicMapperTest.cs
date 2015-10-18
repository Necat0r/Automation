using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;
using Module;

namespace ModuleBaseTest
{
    [TestClass]
    public class DynamicMapperTest
    {
        private class EmptyClass
        {}

        private enum TestEnum
        {
            Value1,
            Value2
        };

        private class TypesClass
        {
            public bool BoolProperty { get; set; }
            public int IntProperty { get; set; }
            public float FloatProperty { get; set; }
            public string StringProperty { get; set; }
            public TestEnum EnumProperty { get; set; }
            public object ObjectProperty { get; set; }

            // Shouldn't interfere
            public int Dummy = 0;
            public int GetDummy() { return Dummy; }
        }

        [TestMethod]
        public void DynamicMapper_TestEmpty()
        {
            var obj = new ExpandoObject();

            DynamicMapper.Map<EmptyClass>(obj);

            // Shouldn't throw
        }

        [TestMethod]
        public void DynamicMapper_TestTypesMapping()
        {
            dynamic obj = new ExpandoObject();
            obj.BoolProperty = true;
            obj.IntProperty = 10;
            obj.FloatProperty = 13.37f;
            obj.StringProperty = "foo";
            obj.EnumProperty = TestEnum.Value2;
            obj.ObjectProperty = new object();

            TypesClass mapped = DynamicMapper.Map<TypesClass>(obj);
            Assert.AreEqual(obj.BoolProperty, mapped.BoolProperty);
            Assert.AreEqual(obj.IntProperty, mapped.IntProperty);
            Assert.AreEqual(obj.FloatProperty, mapped.FloatProperty);
            Assert.AreEqual(obj.StringProperty, mapped.StringProperty);
            Assert.AreEqual(obj.EnumProperty, mapped.EnumProperty);
            Assert.AreEqual(obj.ObjectProperty, mapped.ObjectProperty);
        }

        [TestMethod]
        public void DynamicMapper_TestTypesConversion()
        {
            dynamic obj = new ExpandoObject();
            obj.BoolProperty = "true";
            obj.IntProperty = "10";
            obj.FloatProperty = "13.37";
            obj.StringProperty = "foo";
            obj.EnumProperty = ((int)TestEnum.Value2).ToString();
            obj.ObjectProperty = "";

            TypesClass mapped = DynamicMapper.Map<TypesClass>(obj);

            Assert.AreEqual(true, mapped.BoolProperty);
            Assert.AreEqual(10, mapped.IntProperty);
            Assert.AreEqual(13.37f, mapped.FloatProperty);
            Assert.AreEqual("foo", mapped.StringProperty);
            Assert.AreEqual(TestEnum.Value2, mapped.EnumProperty);
            Assert.AreEqual(obj.ObjectProperty, mapped.ObjectProperty);
        }

        [TestMethod]
        public void DynamicMapper_TestDefaultsMapping()
        {
            dynamic obj = new ExpandoObject();

            TypesClass mapped = DynamicMapper.Map<TypesClass>(obj, true);

            // Shouldn't throw
        }
    }
}
