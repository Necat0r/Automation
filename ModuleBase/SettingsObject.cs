using System;
using System.Collections.Generic;
using System.Dynamic;

namespace ModuleBase
{
    public class SettingsObject : DynamicObject
    {
        private Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 0)
                throw new Exception("Missing index");

            return dictionary.TryGetValue(indexes[0].ToString(), out result);
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length >= 1)
                dictionary[indexes[0].ToString()] = value;

            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return dictionary.TryGetValue(binder.Name.ToLower(), out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            dictionary[binder.Name.ToLower()] = value;
            return true;
        }
    }
}
