using UnityEngine;

namespace Crosstales.FB.Wrapper
{
   /// <summary>Base class for all file browsers.</summary>
   public abstract class BaseFileBrowser : IFileBrowser
   {
      #region Variables

      protected byte[] openSingleFileData;
      protected string lastOpenFile;

      #endregion


      #region Implemented methods

      public abstract bool canOpenFile { get; }
      public abstract bool canOpenFolder { get; }
      public abstract bool canSaveFile { get; }

      public abstract bool canOpenMultipleFiles { get; }

      public abstract bool canOpenMultipleFolders { get; }

      public abstract bool isPlatformSupported { get; }

      public abstract bool isWorkingInEditor { get; }

      //public abstract string CurrentOpenSingleFile { get; set; }
      //public abstract string[] CurrentOpenFiles { get; set; }
      //public abstract string CurrentOpenSingleFolder { get; set; }
      //public abstract string[] CurrentOpenFolders { get; set; }
      //public abstract string CurrentSaveFile { get; set; }
      public virtual string CurrentOpenSingleFile { get; set; }
      public virtual string[] CurrentOpenFiles { get; set; }
      public virtual string CurrentOpenSingleFolder { get; set; }
      public virtual string[] CurrentOpenFolders { get; set; }
      public virtual string CurrentSaveFile { get; set; }

      public virtual byte[] CurrentOpenSingleFileData
      {
         get
         {
            if (!string.IsNullOrEmpty(CurrentOpenSingleFile))
            {
               if (CurrentOpenSingleFile != lastOpenFile)
               {
                  lastOpenFile = CurrentOpenSingleFile;

                  try
                  {
                     openSingleFileData = System.IO.File.ReadAllBytes(CurrentOpenSingleFile);
                  }
                  catch (System.Exception ex)
                  {
                     openSingleFileData = null;

                     //if (Util.Config.DEBUG)
                     Debug.LogWarning($"Could not read file: {CurrentOpenSingleFile} - {ex}");
                  }
               }
            }
            else
            {
               openSingleFileData = null;
            }

            return openSingleFileData;
         }
      }

      public virtual byte[] CurrentSaveFileData { get; set; }

      public string OpenSingleFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         string[] files = OpenFiles(title, directory, defaultName, false, extensions);
         string file = files?.Length > 0 ? files[0] : string.Empty;

         return file;
      }

      public abstract string[] OpenFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions);

      public string OpenSingleFolder(string title, string directory)
      {
         string[] folders = OpenFolders(title, directory, false);
         return folders?.Length > 0 ? folders[0] : string.Empty;
      }

      public abstract string[] OpenFolders(string title, string directory, bool multiselect);

      public abstract string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions);

      public abstract void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, System.Action<string[]> cb);

      public abstract void OpenFoldersAsync(string title, string directory, bool multiselect, System.Action<string[]> cb);

      public abstract void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb);

      #endregion


      #region Protected methods
/*
      protected void resetOpenFiles(params string[] paths)
      {
         CurrentOpenFiles = System.Array.Empty<string>();
         CurrentOpenSingleFile = string.Empty;
      }

      protected void resetOpenFolders(params string[] paths)
      {
         CurrentOpenFolders = System.Array.Empty<string>();
         CurrentOpenSingleFolder = string.Empty;
      }

      protected void resetSaveFile(params string[] paths)
      {
         CurrentSaveFile = string.Empty;
      }
*/
      #endregion
   }
}
// © 2017-2023 crosstales LLC (https://www.crosstales.com)