#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.FB.Util;

namespace Crosstales.FB.EditorUtil
{
   /// <summary>Editor helper class.</summary>
   public abstract class EditorHelper : Crosstales.Common.EditorUtil.BaseEditorHelper
   {
      #region Static variables

      /// <summary>Start index inside the "GameObject"-menu.</summary>
      public const int GO_ID = 26;

      /// <summary>Start index inside the "Tools"-menu.</summary>
      public const int MENU_ID = 11018; // 1, T = 20, R = 18

      private static Texture2D logo_asset;
      private static Texture2D logo_asset_small;

      private static Texture2D icon_file;
      private static Texture2D asset_RTFB;

      #endregion


      #region Static properties

      public static Texture2D Logo_Asset => loadImage(ref logo_asset, "logo_asset_pro.png");

      public static Texture2D Logo_Asset_Small => loadImage(ref logo_asset_small, "logo_asset_small_pro.png");

      public static Texture2D Icon_File => loadImage(ref icon_file, "icon_file.png");

      public static Texture2D Asset_RTFB => loadImage(ref asset_RTFB, "asset_RTFB.png");

      #endregion


      #region Static methods

      /// <summary>Shows an "FileBrowser unavailable"-UI.</summary>
      public static void FBUnavailable()
      {
         EditorGUILayout.HelpBox("File Browser not available!", MessageType.Warning);

         EditorGUILayout.HelpBox($"Did you add the '{Constants.FB_SCENE_OBJECT_NAME}'-prefab to the scene?", MessageType.Info);

         GUILayout.Space(8);

         if (GUILayout.Button(new GUIContent($"Add {Constants.FB_SCENE_OBJECT_NAME}", Icon_Plus, $"Add the '{Constants.FB_SCENE_OBJECT_NAME}'-prefab to the current scene.")))
         {
            InstantiatePrefab(Constants.FB_SCENE_OBJECT_NAME);
         }
      }

      /// <summary>Instantiates a prefab.</summary>
      /// <param name="prefabName">Name of the prefab.</param>
      public static void InstantiatePrefab(string prefabName)
      {
         InstantiatePrefab(prefabName, Crosstales.FB.EditorUtil.EditorConfig.PREFAB_PATH);
      }

      /// <summary>Checks if the 'FileBrowser'-prefab is in the scene.</summary>
      /// <returns>True if the 'FileBrowser'-prefab is in the scene.</returns>
      public static bool isFileBrowserInScene => GameObject.FindObjectOfType(typeof(FileBrowser)) != null; //GameObject.Find(Constants.FB_SCENE_OBJECT_NAME) != null;

      /// <summary>Loads an image as Texture2D from 'Editor Default Resources'.</summary>
      /// <param name="logo">Logo to load.</param>
      /// <param name="fileName">Name of the image.</param>
      /// <returns>Image as Texture2D from 'Editor Default Resources'.</returns>
      private static Texture2D loadImage(ref Texture2D logo, string fileName)
      {
         if (logo == null)
         {
#if CT_DEVELOP
            logo = (Texture2D)AssetDatabase.LoadAssetAtPath($"Assets{Crosstales.FB.EditorUtil.EditorConfig.ASSET_PATH}Icons/{fileName}", typeof(Texture2D));
#else
            logo = (Texture2D)EditorGUIUtility.Load($"crosstales/FileBrowser/{fileName}");
#endif

            if (logo == null)
            {
               Debug.LogWarning($"Image not found: {fileName}");
            }
         }

         return logo;
      }

      #endregion
   }
}
#endif
// © 2019-2023 crosstales LLC (https://www.crosstales.com)