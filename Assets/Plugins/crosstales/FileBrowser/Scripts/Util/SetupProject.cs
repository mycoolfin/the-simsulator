using UnityEngine;

namespace Crosstales.FB.Util
{
   /// <summary>Setup the project to use File Browser.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoadAttribute]
#endif
   public class SetupProject
   {
      #region Constructor

      static SetupProject()
      {
         setup();
      }

      #endregion


      #region Public methods

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void setup()
      {
         Crosstales.Common.Util.Singleton<FileBrowser>.PrefabPath = "Prefabs/FileBrowser";
         Crosstales.Common.Util.Singleton<FileBrowser>.GameObjectName = "FileBrowser";
      }

      #endregion
   }
}
// © 2020-2023 crosstales LLC (https://www.crosstales.com)