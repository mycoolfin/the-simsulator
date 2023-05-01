#if UNITY_EDITOR
using UnityEditor;
using Crosstales.FB.EditorUtil;
using Crosstales.FB.Util;

namespace Crosstales.FB.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class FileBrowserGameObject
   {
      [MenuItem("GameObject/" + Constants.ASSET_NAME + "/" + Constants.FB_SCENE_OBJECT_NAME, false, EditorHelper.GO_ID)]
      private static void AddFB()
      {
         EditorHelper.InstantiatePrefab(Constants.FB_SCENE_OBJECT_NAME);
      }

      [MenuItem("GameObject/" + Constants.ASSET_NAME + "/" + Constants.FB_SCENE_OBJECT_NAME, true)]
      private static bool AddFBValidator()
      {
         return !EditorHelper.isFileBrowserInScene;
      }
   }
}
#endif
// © 2020-2023 crosstales LLC (https://www.crosstales.com)