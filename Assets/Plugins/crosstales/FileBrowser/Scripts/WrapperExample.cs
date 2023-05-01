using UnityEngine;

namespace Crosstales.FB
{
   /// <summary>
   /// Example for a custom wrapper with all callbacks (only for demonstration - it doesn't do anything).
   /// NOTE: please make sure you understand the Wrapper and its variables
   /// </summary>
   [ExecuteInEditMode]
   public class WrapperExample : Crosstales.FB.Wrapper.BaseCustomFileBrowser
   {
      #region Properties

      public override bool canOpenFile
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper can open a file.
            return false;
         }
      }

      public override bool canOpenFolder
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper can open a folder.
            return false;
         }
      }

      public override bool canSaveFile
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper can save a file.
            return false;
         }
      }

      public override bool canOpenMultipleFiles
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper can open multiple files.
            return true;
         }
      }

      public override bool canOpenMultipleFolders
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper can open multiple folders.
            return true;
         }
      }

      public override bool isPlatformSupported
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper is supporting the current platform.
            return true;
         }
      }

      public override bool isWorkingInEditor
      {
         get
         {
            // SKELETON CODE - replace with your own code!
            // Indicates if this wrapper is working directly inside the Unity Editor (without 'Play'-mode).
            return true;
         }
      }

      public override string CurrentOpenSingleFile { get; set; }
      public override string[] CurrentOpenFiles { get; set; }
      public override string CurrentOpenSingleFolder { get; set; }
      public override string[] CurrentOpenFolders { get; set; }
      public override string CurrentSaveFile { get; set; }

      #endregion


      #region Implemented methods

      public override string[] OpenFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions)
      {
         // SKELETON CODE - replace with your own code!
         //resetOpenFiles();

         Debug.Log($"OpenFiles: {title} - {directory} - {defaultName} - {multiselect} - {extensions?.CTDump()}");

         return new[] { "OpenFile.txt" };
      }

      public override string[] OpenFolders(string title, string directory, bool multiselect)
      {
         // SKELETON CODE - replace with your own code!
         //resetOpenFolders();

         Debug.Log($"OpenFolders: {title} - {directory} - {multiselect}");

         return new[] { "OpenFolder" };
      }

      public override string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         // SKELETON CODE - replace with your own code!
         //resetSaveFile();

         Debug.Log($"SaveFile: {title} - {directory} - {defaultName} - {extensions?.CTDump()}");

         return "SaveFile.txt";
      }

      public override void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, System.Action<string[]> cb)
      {
         // SKELETON CODE - replace with your own code!
         //resetOpenFiles();

         Debug.Log($"OpenFilesAsync: {title} - {directory} - {defaultName} - {multiselect} - {cb} - {extensions?.CTDump()}");
      }

      public override void OpenFoldersAsync(string title, string directory, bool multiselect, System.Action<string[]> cb)
      {
         // SKELETON CODE - replace with your own code!
         //resetOpenFolders();

         Debug.Log($"OpenFoldersAsync: {title} - {directory} - {multiselect} - {cb}");
      }

      public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb)
      {
         // SKELETON CODE - replace with your own code!
         //resetSaveFile();

         Debug.Log($"SaveFileAsync: {title} - {directory} - {defaultName} - {cb} - {extensions?.CTDump()}");
      }

      #endregion
   }
}
// © 2020-2023 crosstales LLC (https://www.crosstales.com)