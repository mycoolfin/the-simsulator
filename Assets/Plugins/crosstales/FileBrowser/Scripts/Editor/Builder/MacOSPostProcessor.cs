#if UNITY_EDITOR && (UNITY_STANDALONE_OSX || CT_DEVELOP)
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using Crosstales.FB.EditorUtil;

namespace Crosstales.FB.EditorBuild
{
   /// <summary>Post processor for macOS.</summary>
   public static class MacOSPostProcessor
   {
      private const string id = "com.crosstales.fb";

      [PostProcessBuildAttribute(1)]
      public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
      {
         if (EditorHelper.isMacOSPlatform)
         {
            //remove all meta-files
            string[] files = Crosstales.Common.Util.FileHelper.GetFiles(pathToBuiltProject, true, "meta");

            try
            {
               foreach (string file in files)
               {
                  //Debug.Log(file);
                  System.IO.File.Delete(file);
               }
            }
            catch (System.Exception ex)
            {
               Debug.Log($"Could not delete files: {ex}");
            }

            if (EditorConfig.MACOS_MODIFY_BUNDLE)
            {
               //rewrite Info.plist
               files = Crosstales.Common.Util.FileHelper.GetFiles(pathToBuiltProject, true, "plist");

               try
               {
                  foreach (string file in files)
                  {
                     string content = System.IO.File.ReadAllText(file);

                     if (content.Contains(id))
                     {
                        content = content.Replace(id, $"{id}.{System.DateTime.Now:yyyyMMddHHmmss}");
                        System.IO.File.WriteAllText(file, content);
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.Log($"Could not rewrite 'Info.plist' files: {ex}");
               }
               //UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle("/path/to/bundle.bundle"); //TODO add for Unity > 2018?
            }
         }
      }
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)