#if UNITY_EDITOR && (UNITY_STANDALONE || CT_DEVELOP)
using UnityEditor;
using UnityEngine;

namespace Crosstales.FB.EditorBuild
{
   /// <summary>Sets the required build parameters for Standalone.</summary>
   [InitializeOnLoad]
   public static class SetStandalone
   {
      #region Constructor

      static SetStandalone()
      {
         if (!PlayerSettings.visibleInBackground)
         {
            PlayerSettings.visibleInBackground = true;

            Debug.Log("Standalone: 'visibleInBackground' set to true");
         }
      }

      #endregion
   }
}
#endif
// © 2022-2023 crosstales LLC (https://www.crosstales.com)