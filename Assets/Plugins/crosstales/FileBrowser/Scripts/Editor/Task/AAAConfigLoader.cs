#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.FB.Util;

namespace Crosstales.FB.EditorTask
{
   /// <summary>Loads the configuration at startup.</summary>
   [InitializeOnLoad]
   public static class AAAConfigLoader
   {
      #region Constructor

      static AAAConfigLoader()
      {
         if (!Config.isLoaded)
         {
            Config.Load();

            if (Config.DEBUG)
               Debug.Log("Config data loaded");
         }
      }

      #endregion
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)