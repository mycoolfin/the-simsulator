#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Crosstales.FB.EditorTask
{
   /// <summary>Moves all resources to 'Editor Default Resources'.</summary>
   [InitializeOnLoad]
   public abstract class SetupResources : Crosstales.Common.EditorTask.BaseSetupResources
   {
      #region Variables

      private const string id = "CFBundleIdentifier";

      #endregion


      #region Constructor

      static SetupResources()
      {
         Setup();
      }

      #endregion


      #region Public methods

      public static void Setup()
      {
#if !CT_DEVELOP
         string path = Application.dataPath;

         string assetpath = $"Assets{EditorUtil.EditorConfig.ASSET_PATH}";
         string sourceFolder = $"{path}{EditorUtil.EditorConfig.ASSET_PATH}Icons/";
         string source = $"{assetpath}Icons/";

         string targetFolder = $"{path}/Editor Default Resources/crosstales/FileBrowser/";
         string target = "Assets/Editor Default Resources/crosstales/FileBrowser/";
         string metafile = $"{assetpath}Icons.meta";

         setupResources(source, sourceFolder, target, targetFolder, metafile);
#endif
/*
         if (EditorUtil.EditorHelper.isMacOSPlatform)
         {
            //rewrite Info.plist
            try
            {
               string file = $"{path}{EditorUtil.EditorConfig.ASSET_PATH}Libraries/macOS/FileBrowser.bundle/Contents/Info.plist"; //TODO update if path changes

               System.Collections.Generic.List<string> lines = Crosstales.FB.Util.Helper.SplitStringToLines(System.IO.File.ReadAllText(file));

               for (int ii = 0; ii < lines.Count; ii++)
               {
                  if (lines[ii].Contains(id))
                  {
                     lines[ii + 1] = $"	<string>{PlayerSettings.applicationIdentifier}</string>";
                     break;
                  }
               }

               string content = lines.CTDump();
               //Debug.Log($"New content: {content}");
               System.IO.File.WriteAllText(file, content);
            }
            catch (System.Exception ex)
            {
               Debug.Log($"Could not rewrite 'Info.plist' file: {ex}");
            }
         }
*/
      }

      #endregion
   }
}
#endif
// © 2019-2023 crosstales LLC (https://www.crosstales.com)