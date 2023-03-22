using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CSTG.CodeAnalyzer.Model;

namespace CSTG.CodeAnalyzer
{
    public class ConfigFileUtil
    {
        public static ConfigFile Read(FileInfo configFileInfo)
        {

            var configFile = new ConfigFile
            {
                File = configFileInfo
            };

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(configFileInfo.FullName);
            XmlNodeList connectionStringsRoot = xmlDoc.GetElementsByTagName("connectionStrings");
            XmlNodeList appSettingsRoot = xmlDoc.GetElementsByTagName("appSettings");
            XmlNodeList endpoints = xmlDoc.GetElementsByTagName("endpoint");
            //XmlNodeList serviceModelRoot = xmlDoc.GetElementsByTagName("system.serviceModel");

            if (connectionStringsRoot.Count >= 1)
            {
                foreach (XmlNode child in connectionStringsRoot[0].ChildNodes)
                {
                    if (child.Name == "add")
                    {
                        configFile.ConnectionStrings.Add(
                            new NameValuePair(child.Attributes.GetNamedItem("name")?.Value, child.Attributes.GetNamedItem("connectionString")?.Value));
                    }
                }
            }
            if (appSettingsRoot.Count >= 1)
            {
                foreach (XmlNode child in appSettingsRoot[0].ChildNodes)
                {
                    if (child.Name == "add")
                    {
                        configFile.Settings.Add(
                            new NameValuePair(child.Attributes.GetNamedItem("key")?.Value, child.Attributes.GetNamedItem("value")?.Value));
                    }
                }
            }

            foreach (XmlNode endpoint in endpoints)
            {
                var address = endpoint.Attributes.GetNamedItem("address");
                var name = endpoint.Attributes.GetNamedItem("name");
                if (address != null && name != null)
                {
                    configFile.ServiceModelEndpoints.Add(
                        new NameValuePair(name.Value, address.Value));
                }
            }
            //XmlNodeList mailSettingsRoot = xmlDoc.GetElementsByTagName("mailSettings");
            //if (mailSettingsRoot.Count == 1)
            //{
            //    foreach (XmlNode child in mailSettingsRoot[0].ChildNodes)
            //    {
            //        if (child.Name == "smtp")
            //        {
            //            configFile.Settings.Add(
            //                new NameValuePair(child.Attributes.GetNamedItem("from")?.Value, child.Attributes.GetNamedItem("value")?.Value));
            //            foreach (XmlNode child2ndLevel in child.ChildNodes)
            //            {
            //                if (child.Name == "network")
            //                {
            //                    configFile.Settings.Add(
            //                        new NameValuePair(child.Attributes.GetNamedItem("key")?.Value, child.Attributes.GetNamedItem("value")?.Value));
            //                    configFile.Settings.Add(
            //                        new NameValuePair(child.Attributes.GetNamedItem("key")?.Value, child.Attributes.GetNamedItem("value")?.Value));
            //                    configFile.Settings.Add(
            //                        new NameValuePair(child.Attributes.GetNamedItem("key")?.Value, child.Attributes.GetNamedItem("value")?.Value));
            //                }
            //            }
            //        }
            //    }
            //}
            return configFile;
        }
    }
}
