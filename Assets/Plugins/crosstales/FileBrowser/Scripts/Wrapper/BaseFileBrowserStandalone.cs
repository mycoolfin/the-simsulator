#if UNITY_STANDALONE || UNITY_EDITOR || CT_DEVELOP
using System;
using UnityEngine;

namespace Crosstales.FB.Wrapper
{
   /// <summary>Base class for all standalone file browser implementations.</summary>
   public abstract class BaseFileBrowserStandalone : BaseFileBrowser
   {
      #region Implemented methods

      public override bool canOpenFile => true;
      public override bool canOpenFolder => true;
      public override bool canSaveFile => true;

      public override bool canOpenMultipleFiles => true;

      #endregion

/*
      #region Abstract methods

      protected abstract string[] openFiles(string title, string directory, string defaultName, bool multiselect, bool isAsync, params ExtensionFilter[] extensions);

      protected abstract string[] openFolders(string directory, bool isAsync);

      protected abstract string saveFile(string title, string directory, string defaultName, bool isAsync, params ExtensionFilter[] extensions);

      #endregion
*/
   }
}
#endif
// © 2022-2023 crosstales LLC (https://www.crosstales.com)