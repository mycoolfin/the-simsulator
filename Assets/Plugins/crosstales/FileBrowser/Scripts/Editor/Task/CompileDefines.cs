﻿#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.FB.EditorTask
{
   /// <summary>Adds the given define symbols to PlayerSettings define symbols.</summary>
   [InitializeOnLoad]
   public class CompileDefines : Crosstales.Common.EditorTask.BaseCompileDefines
   {
      private static readonly string[] symbols = { "CT_FB" };

      static CompileDefines()
      {
         if (Crosstales.FB.EditorUtil.EditorConfig.COMPILE_DEFINES)
         {
            addSymbolsToAllTargets(symbols);
         }
         else
         {
            removeSymbolsFromAllTargets(symbols);
         }
      }
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)