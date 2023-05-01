#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.FB.EditorUtil
{
   /// <summary>Editor configuration for the asset.</summary>
   [InitializeOnLoad]
   public static class EditorConfig
   {
      #region Variables

      /// <summary>Enable or disable update-checks for the asset.</summary>
      public static bool UPDATE_CHECK = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_UPDATE_CHECK;

      /// <summary>Enable or disable adding compile define "CT_FB" for the asset.</summary>
      public static bool COMPILE_DEFINES = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_COMPILE_DEFINES;

      /// <summary>Automatically load and add the prefabs to the scene.</summary>
      public static bool PREFAB_AUTOLOAD = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_PREFAB_AUTOLOAD;

      /// <summary>Enable or disable the icon in the hierarchy.</summary>
      public static bool HIERARCHY_ICON = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_HIERARCHY_ICON;

      /// <summary>Enable or disable the modifications of the bundle under macOS.</summary>
      public static bool MACOS_MODIFY_BUNDLE = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_MACOS_MODIFY_BUNDLE;

      /// <summary>Enable or disable the modifications of the Package.appxmanifest under UWP (WSA).</summary>
      public static bool WSA_MODIFY_MANIFEST = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_WSA_MODIFY_MANIFEST;

      /// <summary>Is the configuration loaded?</summary>
      public static bool isLoaded;

      private static string assetPath;
      private const string idPath = "Documentation/id/";
      private static readonly string idName = $"{Crosstales.FB.EditorUtil.EditorConstants.ASSET_UID}.txt";

      #endregion


      #region Constructor

      static EditorConfig()
      {
         if (!isLoaded)
         {
            Load();
         }
      }

      #endregion


      #region Properties

      /// <summary>Returns the path to the asset inside the Unity project.</summary>
      /// <returns>The path to the asset inside the Unity project.</returns>
      public static string ASSET_PATH
      {
         get
         {
            if (assetPath == null)
            {
               try
               {
                  if (System.IO.File.Exists(Application.dataPath + Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_ASSET_PATH + idPath + idName))
                  {
                     assetPath = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_ASSET_PATH;
                  }
                  else
                  {
                     string[] files = System.IO.Directory.GetFiles(Application.dataPath, idName, System.IO.SearchOption.AllDirectories);

                     if (files.Length > 0)
                     {
                        string name = files[0].Substring(Application.dataPath.Length);
                        assetPath = name.Substring(0, name.Length - idPath.Length - idName.Length).Replace("\\", "/");
                     }
                     else
                     {
                        Debug.LogWarning($"Could not locate the asset! File not found: {idName}");
                        assetPath = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_ASSET_PATH;
                     }
                  }

                  Crosstales.Common.Util.CTPlayerPrefs.SetString(Crosstales.FB.Util.Constants.KEY_ASSET_PATH, assetPath);
                  Crosstales.Common.Util.CTPlayerPrefs.Save();
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning($"Could not locate asset: {ex}");
               }
            }

            return assetPath;
         }
      }

      /// <summary>Returns the path of the prefabs.</summary>
      /// <returns>The path of the prefabs.</returns>
      public static string PREFAB_PATH => ASSET_PATH + Crosstales.FB.EditorUtil.EditorConstants.PREFAB_SUBPATH;

      #endregion


      #region Public static methods

      /// <summary>Resets all changeable variables to their default value.</summary>
      public static void Reset()
      {
         UPDATE_CHECK = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_UPDATE_CHECK;
         COMPILE_DEFINES = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_COMPILE_DEFINES;
         PREFAB_AUTOLOAD = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_PREFAB_AUTOLOAD;
         HIERARCHY_ICON = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_HIERARCHY_ICON;
         MACOS_MODIFY_BUNDLE = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_MACOS_MODIFY_BUNDLE;
         WSA_MODIFY_MANIFEST = Crosstales.FB.EditorUtil.EditorConstants.DEFAULT_WSA_MODIFY_MANIFEST;
      }

      /// <summary>Loads the all changeable variables.</summary>
      public static void Load()
      {
         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.FB.EditorUtil.EditorConstants.KEY_UPDATE_CHECK))
            UPDATE_CHECK = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_UPDATE_CHECK);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.FB.EditorUtil.EditorConstants.KEY_COMPILE_DEFINES))
            COMPILE_DEFINES = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_COMPILE_DEFINES);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.FB.EditorUtil.EditorConstants.KEY_PREFAB_AUTOLOAD))
            PREFAB_AUTOLOAD = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_PREFAB_AUTOLOAD);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.FB.EditorUtil.EditorConstants.KEY_HIERARCHY_ICON))
            HIERARCHY_ICON = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_HIERARCHY_ICON);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.FB.EditorUtil.EditorConstants.KEY_MACOS_MODIFY_BUNDLE))
            MACOS_MODIFY_BUNDLE = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_MACOS_MODIFY_BUNDLE);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.FB.EditorUtil.EditorConstants.KEY_WSA_MODIFY_MANIFEST))
            WSA_MODIFY_MANIFEST = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_WSA_MODIFY_MANIFEST);

         isLoaded = true;
      }

      /// <summary>Saves the all changeable variables.</summary>
      public static void Save()
      {
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_UPDATE_CHECK, UPDATE_CHECK);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_COMPILE_DEFINES, COMPILE_DEFINES);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_PREFAB_AUTOLOAD, PREFAB_AUTOLOAD);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_HIERARCHY_ICON, HIERARCHY_ICON);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_MACOS_MODIFY_BUNDLE, MACOS_MODIFY_BUNDLE);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.FB.EditorUtil.EditorConstants.KEY_WSA_MODIFY_MANIFEST, WSA_MODIFY_MANIFEST);

         Crosstales.Common.Util.CTPlayerPrefs.Save();
      }

      #endregion
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)