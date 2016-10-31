using Logging;
using Microsoft.CSharp.RuntimeBinder;
using Module;
using ModuleBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Xml;

namespace Automation
{
    public sealed class Settings
    {

        //<settings>
        //    <services>
        //        <service name="dummy" dll="dummy.dll">
        //            <active">4</active>
        //            <port>4</port>
        //        </service>
        //    </services>
        //    <devices>
        //        <device name="lamp_bedroom" type="RfxCom.Lighting5">
        //            <code>12345678</code>
        //        </device>
        //    </devices>
        //</settings>

        private readonly XmlDocument mDocument = new XmlDocument();

        public Settings(string path, string filename)
        {
            path.TrimEnd(new char[] { '\\' });

            var fullName = Path.Combine(path, filename);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (!File.Exists(fullName))
            {
                throw new InvalidDataException("Missing settings file: " + fullName);
            }
            else
                mDocument.Load(fullName);
        }

        public SettingsObject LoadSettingsObject(XmlNode node)
        {
            var settings = new SettingsObject();

            // Copy in attributes
            foreach (XmlAttribute attribute in node.Attributes)
                settings[attribute.Name.ToLower()] = attribute.Value;

            if (node.InnerText.Length > 0)
                settings["value"] = node.InnerText;

            bool hasChildren = node.ChildNodes.Count > 0 && node.FirstChild.GetType() != typeof(XmlText);
            if (!hasChildren)
                return settings;

            // Parse children & set up values
            foreach (XmlNode child in node.ChildNodes)
            {
                string name = child.Name.ToLower();

                // Disregard any XmlText nodes since they're not a proper child.
                bool hasGrandChildren = child.ChildNodes.Count > 0 && child.FirstChild.GetType() != typeof(XmlText);
                bool isArray = hasGrandChildren && child.Attributes.Count == 0;
                if (hasGrandChildren)
                {
                    // Detect if we should fold a subtree into an array
                    foreach (XmlNode grandChild in child.ChildNodes)
                    {
                        string grandChildName = grandChild.Name.ToLower();

                        // Assume all grand children in an array will be named '<name>' under a parent named '<name>s'
                        if (grandChildName + "s" != name)
                        {
                            isArray = false;
                            break;
                        }
                    }
                }

                if (isArray)
                {
                    // Fold array items into a list
                    var list = new List<object>();
                    foreach (XmlNode subchild in child.ChildNodes)
                        list.Add(LoadSettingsObject(subchild));
                    settings[name] = list;
                }
                else if (hasGrandChildren || child.Attributes.Count > 0)
                {
                    // Treat this as a sub-object & recursively process it.
                    settings[name] = LoadSettingsObject(child);
                }
                else
                {
                    // Normal value, fold it into current object directly
                    settings[name] = child.InnerText;
                }
            }

            return settings;
        }

        public Dictionary<string, string> GetConfig()
        {
            XmlNode node = mDocument.SelectSingleNode("settings/config");
            if (node != null)
                return GetValues(node);

            return new Dictionary<string, string>();
        }

        private Dictionary<string, string> GetValues(XmlNode node)
        {
            var values = new Dictionary<string, string>();

            foreach (XmlNode valueNode in node.SelectNodes("value"))
            {
                XmlAttribute name = valueNode.Attributes["name"];
                values.Add(name.Value, valueNode.InnerText);
            }

            return values;
        }

        public List<dynamic> GetServiceConfigs()
        {
            var configs = new List<dynamic>();

            XmlNodeList serviceNodeList = mDocument.SelectNodes("settings/services/service");
            foreach (XmlNode serviceNode in serviceNodeList)
            {
                configs.Add(LoadSettingsObject(serviceNode));
            }

            return configs;
        }

        public List<dynamic> GetDeviceConfigs()
        {
            var configs = new List<dynamic>();

            XmlNodeList deviceNodeList = mDocument.SelectNodes("settings/devices/device");
            foreach (XmlNode deviceNode in deviceNodeList)
            {
                configs.Add(LoadSettingsObject(deviceNode));
            }

            return configs;
        }
    }
}
