using Module;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Runtime.Remoting;

namespace Module
{
    public class DynamicMapper
    {
        static public T Map<T>(dynamic obj, bool allowDefaults = false)
        {
            var dictionary = ((IDictionary<string, object>)obj);
            return Map<T>(obj, allowDefaults);

        }

        static public T Map<T>(IDictionary<string, object> obj, bool allowDefaults = false)
        {
            T target = Activator.CreateInstance<T>();

            foreach (var property in typeof(T).GetProperties())
            {
                // Don't care if property cannot be assigned to
                if (property.SetMethod == null)
                    continue;

                var objectDictionary = ((IDictionary<string, object>)obj);

                if (!objectDictionary.ContainsKey(property.Name))
                {
                    if (allowDefaults)
                        continue;

                    throw new ServiceBase.RequestException(string.Format("Missing key '{0}'.", property.Name));
                }

                Type propertyType = property.PropertyType;
                object value = objectDictionary[property.Name];
                Type valueType = value.GetType();

                if (valueType.IsAssignableFrom(propertyType) || propertyType == typeof(object))
                {
                    // Simple 1:1 mapping
                    property.SetValue(target, value);
                }
                else if (value is string || value is IDynamicMetaObjectProvider)
                {
                    // String mapping
                    string stringValue = value as string;

                    if (propertyType == typeof(System.Int32))
                        property.SetValue(target, Int32.Parse(stringValue));
                    else if (propertyType == typeof(string))
                        property.SetValue(target, stringValue);
                    else if (propertyType == typeof(bool))
                        property.SetValue(target, bool.Parse(stringValue));
                    else if (propertyType == typeof(float))
                    {
                        property.SetValue(target, float.Parse(stringValue, CultureInfo.InvariantCulture));
                    }
                    else if (propertyType.IsEnum)
                    {
                        var intValue = Int32.Parse(stringValue);
                        property.SetValue(target, Enum.ToObject(propertyType, intValue));
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // TODO - Add proper support for lists.
                    }
                    else
                        throw new ArgumentException("Unhandled type for string: " + propertyType.Name);
                }
                else
                    throw new ArgumentException("Unhandled type: " + propertyType.Name);
            }

            return target;
        }
    }
}
