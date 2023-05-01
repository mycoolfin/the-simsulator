using UnityEngine;
using System;

namespace Crosstales.FB.Wrapper
{
   /// <summary>File browser implementation for generic devices (currently NOT IMPLEMENTED).</summary>
   public class FileBrowserGeneric : BaseFileBrowser
   {
      #region Implemented methods

      public override bool canOpenFile => false;
      public override bool canOpenFolder => false;
      public override bool canSaveFile => false;

      public override bool canOpenMultipleFiles => false;

      public override bool canOpenMultipleFolders => false;

      public override bool isPlatformSupported => false;

      public override bool isWorkingInEditor => true;

      public override string[] OpenFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions)
      {
         Debug.LogWarning("'OpenFiles' is currently not supported for the current platform!");
         return null;
      }

      public override string[] OpenFolders(string title, string directory, bool multiselect)
      {
         Debug.LogWarning("'OpenFolders' is currently not supported for the current platform!");
         return null;
      }

      public override string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         Debug.LogWarning("'SaveFile' is currently not supported for the current platform!");
         return null;
      }

      public override void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, Action<string[]> cb)
      {
         cb.Invoke(OpenFiles(title, directory, defaultName, multiselect, extensions));
      }

      public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
      {
         cb.Invoke(OpenFolders(title, directory, multiselect));
      }

      public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
      {
         cb.Invoke(SaveFile(title, directory, defaultName, extensions));
      }

      #endregion
   }
}
// © 2017-2023 crosstales LLC (https://www.crosstales.com)