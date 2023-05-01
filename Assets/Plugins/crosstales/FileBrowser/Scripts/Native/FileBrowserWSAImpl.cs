#if UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT //|| CT_DEVELOP
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.ViewManagement;
using Enumerable = System.Linq.Enumerable;
using UnityEngine;

namespace Crosstales.FB
{
   /// <summary>File browser for WSA.</summary>
   public class FileBrowserWSAImpl
   {
      #region Variables

      public static PickerLocationId CurrentLocation = PickerLocationId.ComputerFolder;
      public static PickerViewMode CurrentViewMode = PickerViewMode.List;

      public static StorageFolder LastOpenFolder;
      public static StorageFile LastSaveFile;

      private static readonly List<StorageFile> lastOpenFiles = new List<StorageFile>();
      private static readonly List<StorageFile> lastGetFiles = new List<StorageFile>();
      private static readonly List<StorageFolder> lastGetDirectories = new List<StorageFolder>();
      private static readonly List<StorageFolder> lastGetDrives = new List<StorageFolder>();
      private readonly List<string> selection = new List<string>();

      private bool useExtentsion;

      #endregion


      #region Properties

      /// <summary>Selected files or folders</summary>
      /// <returns>Selected files or folders</returns>
      public List<string> Selection => selection;

      /// <summary>Last opened files</summary>
      /// <returns>Last opened files</returns>
      public static List<StorageFile> LastOpenFiles => lastOpenFiles;

      /// <summary>Last opened file</summary>
      /// <returns>Last opened file</returns>
      public static StorageFile LastOpenFile => (lastOpenFiles.Count > 0) ? lastOpenFiles[0] : null;

      /// <summary>Last searched files</summary>
      /// <returns>Last searched files</returns>
      public static List<StorageFile> LastGetFiles => lastGetFiles;

      /// <summary>Last searched folders</summary>
      /// <returns>Last searched folders</returns>
      public static List<StorageFolder> LastGetDirectories => lastGetDirectories;

      /// <summary>Last searched drives</summary>
      /// <returns>Last searched drives</returns>
      public static List<StorageFolder> LastGetDrives => lastGetDrives;

      public static bool canOpenMultipleFiles => true;

      public static bool canOpenMultipleFolders => false;


      /// <summary>Indicates if the FB is currently busy.</summary>
      /// <returns>True if the FB is currently busy</returns>
      public bool isBusy { get; set; }

      #endregion


      #region Public Methods

      public async void OpenFiles(List<Extension> extensions, bool multiselect)
      {
         if (EnsureUnsnapped())
         {
            //Debug.Log("OpenFiles...");

            selection.Clear();
            lastOpenFiles.Clear();
            isBusy = true;

            try
            {
               FileOpenPicker openPicker = new FileOpenPicker();
               openPicker.ViewMode = CurrentViewMode;
               openPicker.SuggestedStartLocation = CurrentLocation;

               foreach (Extension extension in extensions)
               {
                  foreach (string ext in extension.Extensions)
                  {
                     //Debug.Log( $"ext: {ext}");

                     openPicker.FileTypeFilter.Add(ext.StartsWith("*") ? ext : $".{ext}");
                  }
               }
               //openPicker.FileTypeFilter.Add(".jpg");

               if (multiselect)
               {
                  IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();

                  if (files.Count > 0)
                  {
                     foreach (StorageFile file in files)
                     {
                        selection.Add(file.Path);
                        lastOpenFiles.Add(file);
                     }
                  }
               }
               else
               {
                  StorageFile file = await openPicker.PickSingleFileAsync();

                  if (file != null)
                  {
                     selection.Add(file.Path);
                     lastOpenFiles.Add(file);
                  }
               }
            }
            catch (Exception ex)
            {
               Debug.LogError(ex);
            }

            //Debug.Log($"OpenFiles end: {selection.Count}");
         }
         else
         {
            Debug.LogError("OpenFiles: could not unsnap!");
         }

         isBusy = false;
      }

      public async void OpenSingleFolder()
      {
         if (EnsureUnsnapped())
         {
            //Debug.Log("OpenSingleFolder...");

            selection.Clear();
            isBusy = true;

            try
            {
               FolderPicker folderPicker = new FolderPicker();
               folderPicker.ViewMode = CurrentViewMode;
               folderPicker.SuggestedStartLocation = CurrentLocation;
               folderPicker.FileTypeFilter.Add("*");

               StorageFolder folder = await folderPicker.PickSingleFolderAsync();

               if (folder != null)
               {
                  selection.Add(folder.Path);
                  LastOpenFolder = folder;
                  StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
               }
            }
            catch (Exception ex)
            {
               Debug.LogError(ex);
            }

            //Debug.Log($"OpenSingleFolder end: {selection.Count}");
         }
         else
         {
            Debug.LogError("OpenSingleFolder: could not unsnap!");
         }

         isBusy = false;
      }

      public async void SaveFile(string defaultName, List<Extension> extensions)
      {
         if (EnsureUnsnapped())
         {
            //Debug.Log( "SaveFile...");

            selection.Clear();
            isBusy = true;

            try
            {
               FileSavePicker savePicker = new FileSavePicker();
               savePicker.SuggestedStartLocation = CurrentLocation;

               foreach (Extension extension in extensions)
               {
                  List<string> exts = new List<string>();

                  foreach (string ext in extension.Extensions)
                  {
                     //Debug.Log( $"ext: {ext}");

                     if (ext.Equals("*"))
                     {
                        exts.Add(".");
                     }
                     else
                     {
                        exts.Add(ext.StartsWith("*") ? ext : $".{ext}");
                     }
                  }

                  savePicker.FileTypeChoices.Add(extension.Name, exts);
               }

               savePicker.SuggestedFileName = defaultName;
               StorageFile file = await savePicker.PickSaveFileAsync();

               if (file != null)
               {
                  selection.Add(file.Path);
                  LastSaveFile = file;
               }
            }
            catch (Exception ex)
            {
               Debug.LogError(ex);
            }

            //Debug.Log($"SaveFile end: {selection.Count}");
         }
         else
         {
            Debug.LogError("SaveFile: could not unsnap!");
         }

         isBusy = false;
      }

      public async void GetDrives()
      {
         //Debug.Log("GetDrives...");

         selection.Clear();
         lastGetDrives.Clear();
         isBusy = true;

         try
         {
            var removableDevices = KnownFolders.RemovableDevices;
            var folders = await removableDevices.GetFoldersAsync();

            foreach (var folder in folders)
            {
               selection.Add(folder.Path);
               lastGetDrives.Add(folder);
            }
         }
         catch (UnauthorizedAccessException)
         {
            Debug.LogError("Access is denied. Did you add the capability 'broadFileSystemAccess' to the 'package.appxmanifest'?");
         }
         catch (Exception ex)
         {
            Debug.LogError(ex);
         }

         //Debug.Log($"GetDrives end: {selection.Count}");

         isBusy = false;
      }

      public async void GetDirectories(string path, bool isRecursive = false)
      {
         //Debug.Log("GetDirectories...");

         selection.Clear();
         lastGetDirectories.Clear();
         isBusy = true;

         try
         {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();

            // Recurse sub-directories.
            await getDirectories(folders, isRecursive, false);
         }
         catch (UnauthorizedAccessException)
         {
            Debug.LogError($"Access to the path '{path}' is denied. Did you add the capability 'broadFileSystemAccess' to the 'package.appxmanifest'?");
         }
         catch (Exception ex)
         {
            Debug.LogError(ex);
         }

         //Debug.Log($"GetDirectories end: {selection.Count}");

         isBusy = false;
      }

      public async void GetFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         useExtentsion = true;
         await getFiles(path, isRecursive, extensions);
      }

      public async void GetFilesForName(string path, bool isRecursive = false, params string[] filenames)
      {
         useExtentsion = false;
         await getFiles(path, isRecursive, filenames);
      }

      /*
              void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, System.Action<string[]> cb);

              void OpenFoldersAsync(string title, string directory, bool multiselect, System.Action<string[]> cb);

              void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb);
      */

      #endregion


      #region Private Methods

      private async System.Threading.Tasks.Task getFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         //Debug.Log("getFiles...");

         selection.Clear();
         lastGetDirectories.Clear();
         lastGetFiles.Clear();
         isBusy = true;

         try
         {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();

            // Recurse sub-directories.
            await getDirectories(folders, isRecursive, true, extensions);

            // Get the files in this folder.
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
               if (isValidFile(file, extensions))
               {
                  //Debug.Log($"File: {file.Path}");

                  selection.Add(file.Path);
                  lastGetFiles.Add(file);
               }
            }
         }
         catch (UnauthorizedAccessException)
         {
            Debug.LogError($"Access to the path '{path}' is denied. Did you add the capability 'broadFileSystemAccess' to the 'package.appxmanifest'?");
         }
         catch (Exception ex)
         {
            Debug.LogError(ex);
         }

         //Debug.Log($"getFiles end: {selection.Count}");

         isBusy = false;
      }

      private async System.Threading.Tasks.Task getDirectories(IReadOnlyList<StorageFolder> folders, bool isRecursive, bool addFiles, params string[] extensions)
      {
         foreach (StorageFolder folder in folders)
         {
            if (!addFiles)
            {
               //Debug.Log($"Folder: {folder.Path}");

               selection.Add(folder.Path);
               lastGetDirectories.Add(folder);
            }

            // Recurse this folder to get sub-folder info.
            IReadOnlyList<StorageFolder> subDir = await folder.GetFoldersAsync();

            if (subDir.Count > 0 && isRecursive)
               await getDirectories(subDir, isRecursive, addFiles, extensions);

            if (addFiles && isRecursive)
            {
               // Get the files in this folder.
               IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

               foreach (StorageFile file in files)
               {
                  if (isValidFile(file, extensions))
                  {
                     //Debug.Log($"File: {file.Path}");

                     selection.Add(file.Path);
                     lastGetFiles.Add(file);
                  }
               }
            }
         }
      }

      private bool isValidFile(StorageFile file, params string[] extensions)
      {
         if (extensions == null || extensions.Length == 0)
            return true;

         if (Enumerable.Any(extensions, extension => extension.Equals("*") || extension.Equals("*.*")))
            return true;

         string filename = file.Path;

         return Enumerable.Any(extensions, extension => (useExtentsion && filename.CTEndsWith(extension)) || (!useExtentsion && filename.CTContains(extension)));
      }

      internal bool EnsureUnsnapped()
      {
         // FilePicker APIs will not work if the application is in a snapped state.
         // If an app wants to show a FilePicker while snapped, it must attempt to unsnap first
         // Note: enable for Windows 8.1
         //bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
         bool unsnapped = true;

         return unsnapped;
      }

      #endregion
   }

   public struct Extension
   {
      public string Name;
      public string[] Extensions;

      public Extension(string filterName, params string[] filterExtensions)
      {
         Name = filterName;
         Extensions = filterExtensions;
      }

      public override string ToString()
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();

         result.Append(GetType().Name);
         result.Append(" {");

         result.Append("Name='");
         result.Append(Name);
         result.Append("', ");

         result.Append("Extensions='");
         result.Append(Extensions.Length);
         result.Append("'");

         result.Append("}");

         return result.ToString();
      }
   }
}
#endif
// © 2018-2023 crosstales LLC (https://www.crosstales.com)