#if UNITY_EDITOR //&& !CT_RTFB
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Crosstales.Common.EditorTask
{
   /// <summary>Search for the "Runtime File Browser" and add the compile define "CT_RTFB".</summary>
   public class RTFBCheck : AssetPostprocessor
   {
      private static readonly string define = "CT_RTFB";
      private static readonly string identifier = "SimpleFileBrowser.aar";

      public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
      {
         if (importedAssets.Any(str => str.Contains("RTFBCheck.cs")))
         {
            //Debug.Log("Searching for RTFB!");

            string[] files = Crosstales.Common.Util.FileHelper.GetFilesForName(Crosstales.Common.Util.BaseConstants.APPLICATION_PATH, true, identifier);
            //string[] files = Crosstales.Common.Util.FileHelper.GetFiles(Crosstales.Common.Util.BaseConstants.APPLICATION_PATH, true, "aar");

            if (files?.Length > 0)
            {
               //Debug.Log("RTFB found!");

               BaseCompileDefines.AddSymbolsToAllTargets(define);
               /*
               if (files.Any(str => str.Contains(identifier)))
               {
                  //Debug.Log("RTFB found!");
                  BaseCompileDefines.AddSymbolsToAllTargets(define);
               }
               else
               {
                  Debug.LogWarning("RTFB not found!");
               }
               */
            }
            /*
            else
            {
               Debug.LogWarning("RTFB not found!");
            }
            */
         }
         else
         {
            if (importedAssets.Any(str => str.Contains(identifier)))
            {
               //Debug.Log("RTFB installed!");
               BaseCompileDefines.AddSymbolsToAllTargets(define);
            }
         }
      }
   }
}
#endif
// © 2022-2023 crosstales LLC (https://www.crosstales.com)