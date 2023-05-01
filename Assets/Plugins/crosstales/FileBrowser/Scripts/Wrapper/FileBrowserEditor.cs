#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace Crosstales.FB.Wrapper
{
   public class FileBrowserEditor : BaseFileBrowser
   {
      #region Implemented methods

      public override bool canOpenFile => true;
      public override bool canOpenFolder => true;
      public override bool canSaveFile => true;

      public override bool canOpenMultipleFiles => false;

      public override bool canOpenMultipleFolders => false;

      public override bool isPlatformSupported => Crosstales.FB.Util.Helper.isWindowsPlatform || Crosstales.FB.Util.Helper.isMacOSPlatform || Crosstales.FB.Util.Helper.isLinuxPlatform || Crosstales.FB.Util.Helper.isWSABasedPlatform;

      public override bool isWorkingInEditor => true;

      public override string[] OpenFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions)
      {
         if (Crosstales.FB.Util.Helper.isMacOSEditor && extensions?.Length > 1)
            Debug.LogWarning("Multiple 'extensions' are not supported in the Editor.");

         if (multiselect)
            Debug.LogWarning("'multiselect' for files is not supported in the Editor.");

         if (!string.IsNullOrEmpty(defaultName))
            Debug.LogWarning("'defaultName' is not supported in the Editor.");

         //resetOpenFiles();

         string path = string.Empty;

         path = extensions == null ? EditorUtility.OpenFilePanel(title, directory, string.Empty) : EditorUtility.OpenFilePanelWithFilters(title, directory, getFilterFromFileExtensionList(extensions));

         if (string.IsNullOrEmpty(path))
         {
            CurrentOpenFiles = System.Array.Empty<string>();
            CurrentOpenSingleFile = string.Empty;

            return null;
         }

         CurrentOpenSingleFile = Crosstales.Common.Util.FileHelper.ValidateFile(path);
         CurrentOpenFiles = new[] { CurrentOpenSingleFile };

         return CurrentOpenFiles;
      }

      public override string[] OpenFolders(string title, string directory, bool multiselect)
      {
         if (multiselect)
            Debug.LogWarning("'multiselect' for folders is not supported in the Editor.");

         //resetOpenFolders();

         string path = EditorUtility.OpenFolderPanel(title, directory, string.Empty);

         if (string.IsNullOrEmpty(path))
         {
            CurrentOpenFolders = System.Array.Empty<string>();
            CurrentOpenSingleFolder = string.Empty;

            return null;
         }

         CurrentOpenSingleFolder = Crosstales.Common.Util.FileHelper.ValidatePath(path);
         CurrentOpenFolders = new[] { CurrentOpenSingleFolder };

         return CurrentOpenFolders;
      }

      public override string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         //resetSaveFile();

         string ext = extensions?.Length > 0 ? extensions[0].Extensions[0].Equals("*") ? string.Empty : extensions[0].Extensions[0] : string.Empty;
         string name = string.IsNullOrEmpty(ext) ? defaultName : $"{defaultName}.{ext}";

         if (extensions?.Length > 1)
            Debug.LogWarning($"Multiple 'extensions' are not supported in the Editor! Using only the first entry '{ext}'.");

         string path = EditorUtility.SaveFilePanel(title, directory, name, ext);

         if (string.IsNullOrEmpty(path))
         {
            CurrentSaveFile = string.Empty;

            return null;
         }

         CurrentSaveFile = Crosstales.Common.Util.FileHelper.ValidateFile(path);

         return CurrentSaveFile;
      }

      public override void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, Action<string[]> cb)
      {
         Debug.LogWarning("'OpenFilesAsync' is running synchronously in the Editor.");
         cb?.Invoke(OpenFiles(title, directory, defaultName, multiselect, extensions));
      }

      public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
      {
         Debug.LogWarning("'OpenFoldersAsync' is running synchronously in the Editor.");
         cb?.Invoke(OpenFolders(title, directory, multiselect));
      }

      public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
      {
         Debug.LogWarning("'SaveFileAsync' is running synchronously in the Editor.");
         cb?.Invoke(SaveFile(title, directory, defaultName, extensions));
      }

      #endregion


      #region Private methods

      private static string[] getFilterFromFileExtensionList(ExtensionFilter[] extensions)
      {
         if (extensions?.Length > 0)
         {
            string[] filters = new string[extensions.Length * 2];

            for (int ii = 0; ii < extensions.Length; ii++)
            {
               filters[ii * 2] = extensions[ii].Name;
               filters[ii * 2 + 1] = string.Join(",", extensions[ii].Extensions);
            }

            if (Crosstales.FB.Util.Config.DEBUG)
               Debug.Log($"getFilterFromFileExtensionList: {filters.CTDump()}");

            return filters;
         }

         return Array.Empty<string>();
      }

      #endregion
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)