#if UNITY_EDITOR && !CT_DJ && (UNITY_WSA || CT_DEVELOP)
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using Enumerable = System.Linq.Enumerable;
using Crosstales.FB.EditorUtil;

namespace Crosstales.FB.EditorBuild
{
   /// <summary>Post processor for UWP (WSA).</summary>
   public static class UWPPostProcessor
   {
      [PostProcessBuild(1)]
      public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
      {
         if (EditorHelper.isWSAPlatform && EditorConfig.WSA_MODIFY_MANIFEST)
         {
            string file = $"{pathToBuiltProject}/{Application.productName}/Package.appxmanifest";
            //Debug.Log($"File: {file}");

            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.Load(file);

            if (!Enumerable.Any(Enumerable.Cast<System.Xml.XmlAttribute>(xmlDoc.DocumentElement.Attributes), child => child.Name.CTEquals("xmlns:rescap")))
            {
               xmlDoc.DocumentElement.SetAttribute("xmlns:rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities");
               xmlDoc.DocumentElement.SetAttribute("IgnorableNamespaces", "uap uap2 uap3 uap4 mp mobile iot rescap");
            }

            System.Xml.XmlNode capabilities = Enumerable.FirstOrDefault(Enumerable.Cast<System.Xml.XmlNode>(xmlDoc.DocumentElement.ChildNodes), child => child.Name.CTEquals("Capabilities"));

            if (capabilities == null)
            {
               capabilities = xmlDoc.CreateElement("Capabilities");
               xmlDoc.DocumentElement.AppendChild(capabilities);
            }

            if (capabilities == null || !Enumerable.Any(Enumerable.Cast<System.Xml.XmlNode>(capabilities.ChildNodes), child => child.Name.CTEquals("rescap:Capability")))
            {
               System.Xml.XmlElement capabilityBfsa = xmlDoc.CreateElement("rescap", "Capability", "");
               capabilityBfsa.SetAttribute("Name", "broadFileSystemAccess");
               capabilities.AppendChild(capabilityBfsa);

               System.Xml.XmlElement capabilityClose = xmlDoc.CreateElement("rescap", "Capability", "");
               capabilityClose.SetAttribute("Name", "confirmAppClose");
               capabilities.AppendChild(capabilityClose);

               System.Xml.XmlElement capabilityRemoveableStorage = xmlDoc.CreateElement("uap", "Capability", "");
               capabilityClose.SetAttribute("Name", "removableStorage");
               capabilities.AppendChild(capabilityRemoveableStorage);
            }

            xmlDoc.Save(file);

            //TODO dirty hack, improve in the future!
            string content = System.IO.File.ReadAllText(file);
            content = content.Replace("<Capabilities xmlns=\"\">", "<Capabilities>");
            content = content.Replace("<Capability Name=\"broadFileSystemAccess\" />", "<rescap:Capability Name=\"broadFileSystemAccess\" />");
            content = content.Replace("<Capability Name=\"confirmAppClose\" xmlns=\"\" />", "<rescap:Capability Name=\"confirmAppClose\" xmlns=\"\" />");
            content = content.Replace("<Capability Name=\"removableStorage\" />", "<uap:Capability Name=\"removableStorage\" />");
            System.IO.File.WriteAllText(file, content);
         }
      }
   }
}
#endif
// © 2021-2023 crosstales LLC (https://www.crosstales.com)