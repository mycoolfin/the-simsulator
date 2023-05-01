using System.Linq;
using UnityEngine;
using Crosstales.FB.Util;
using Crosstales.FB.Wrapper;

namespace Crosstales.FB
{
   /// <summary>Native file browser various actions like open file, open folder and save file.</summary>
   [ExecuteInEditMode]
   [DisallowMultipleComponent]
   [HelpURL("https://www.crosstales.com/media/data/assets/FileBrowser/api/class_crosstales_1_1_f_b_1_1_file_browser.html")]
   public class FileBrowser : Crosstales.Common.Util.Singleton<FileBrowser>
   {
      #region Variables

      [Header("Custom Wrapper"), Tooltip("Custom wrapper for File Browser."), SerializeField] private BaseCustomFileBrowser customWrapper;

      [Tooltip("Enable or disable the custom wrapper (default: false)."), SerializeField] private bool customMode;

      [Header("Titles")] [Tooltip("Title for the 'Open File'-dialog."), SerializeField] private string titleOpenFile = "Open File";
      [Tooltip("Title for the 'Open Files'-dialog."), SerializeField] private string titleOpenFiles = "Open Files";
      [Tooltip("Title for the 'Open Folder'-dialog."), SerializeField] private string titleOpenFolder = "Select Folder";
      [Tooltip("Title for the 'Open Folders'-dialog."), SerializeField] private string titleOpenFolders = "Select Folders";
      [Tooltip("Title for the 'Save File'-dialog."), SerializeField] private string titleSaveFile = "Save File";

      [Header("Labels")] [Tooltip("Text for 'All Files'-filter (*)"), SerializeField] private string textAllFiles = "All Files";
      [Tooltip("Default name of the save-file."), SerializeField] private string nameSaveFile = "MySaveFile";

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("LegacyFolderBrowser")] [Header("Windows Settings"), Tooltip("Use the legacy folder browser under Windows (default: false)."), SerializeField]
      private bool legacyFolderBrowser;

      [Tooltip("Ask to overwrite existing file in save dialog (default: true)."), SerializeField] private bool askOverwriteFile = true;

      [Header("UWP (WSA) Settings"), Tooltip("Always read the file data under UWP (default: false)."), SerializeField]
      private bool alwaysReadFile;

      private static string lastOpenSingleFile;
      private static string[] lastOpenFiles;
      private static string lastOpenSingleFolder;
      private static string[] lastOpenFolders;
      private static string lastSaveFile;

      private WrapperHolder wrapperHolder;

      #endregion


      #region Properties

      /// <summary>Custom wrapper for File Browser.</summary>
      public BaseCustomFileBrowser CustomWrapper
      {
         get => customWrapper;
         set
         {
            if (customWrapper == value) return;

            customWrapper = value;

            wrapperHolder = new WrapperHolder();
         }
      }

      /// <summary>Enables or disables the custom wrapper.</summary>
      public bool CustomMode
      {
         get => customMode;
         set
         {
            if (customMode == value) return;

            customMode = value;

            wrapperHolder = new WrapperHolder();
         }
      }

      /// <summary>Use the legacy folder browser (Windows).</summary>
      public bool LegacyFolderBrowser
      {
         get => legacyFolderBrowser;
         set => legacyFolderBrowser = value;
      }

      /// <summary>Ask to overwrite existing file in save dialog (Windows).</summary>
      public bool AskOverwriteFile
      {
         get => askOverwriteFile;
         set => askOverwriteFile = value;
      }

      /// <summary>Always read the file data (UWP).</summary>
      public bool AlwaysReadFile
      {
         get => alwaysReadFile;
         set => alwaysReadFile = value;
      }

      /// <summary>Title for the 'Open File'-dialog.</summary>
      public string TitleOpenFile
      {
         get => titleOpenFile;
         set => titleOpenFile = value;
      }

      /// <summary>Title for the 'Open Files'-dialog.</summary>
      public string TitleOpenFiles
      {
         get => titleOpenFiles;
         set => titleOpenFiles = value;
      }

      /// <summary>Title for the 'Open Folder'-dialog.</summary>
      public string TitleOpenFolder
      {
         get => titleOpenFolder;
         set => titleOpenFolder = value;
      }

      /// <summary>Title for the 'Open Folders'-dialog.</summary>
      public string TitleOpenFolders
      {
         get => titleOpenFolders;
         set => titleOpenFolders = value;
      }

      /// <summary>Title for the 'Save File'-dialog.</summary>
      public string TitleSaveFile
      {
         get => titleSaveFile;
         set => titleSaveFile = value;
      }

      /// <summary>Text for 'All Files'-filter (*).</summary>
      public string TextAllFiles
      {
         get => textAllFiles;
         set => textAllFiles = value;
      }

      /// <summary>Default name of the save-file.</summary>
      public string NameSaveFile
      {
         get => nameSaveFile;
         set => nameSaveFile = value;
      }

      /// <summary>Returns the file from the last "OpenSingleFile"-action.</summary>
      /// <returns>File from the last "OpenSingleFile"-action.</returns>
      public string CurrentOpenSingleFile
      {
         get => wrapperHolder?.PlatformWrapper?.CurrentOpenSingleFile;
         set => wrapperHolder.PlatformWrapper.CurrentOpenSingleFile = value;
      }

      /// <summary>Returns the file name (without path) from the last "OpenSingleFile"-action.</summary>
      /// <returns>File name from the last "OpenSingleFile"-action.</returns>
      public string CurrentOpenSingleFileName => getNameFromPath(CurrentOpenSingleFile);

      /// <summary>Returns the array of files from the last "OpenFiles"-action.</summary>
      /// <returns>Array of files from the last "OpenFiles"-action.</returns>
      public string[] CurrentOpenFiles
      {
         get => wrapperHolder?.PlatformWrapper?.CurrentOpenFiles;
         set => wrapperHolder.PlatformWrapper.CurrentOpenFiles = value;
      }

      /// <summary>Returns the folder from the last "OpenSingleFolder"-action.</summary>
      /// <returns>Folder from the last "OpenSingleFolder"-action.</returns>
      public string CurrentOpenSingleFolder
      {
         get => wrapperHolder?.PlatformWrapper?.CurrentOpenSingleFolder;
         set => wrapperHolder.PlatformWrapper.CurrentOpenSingleFolder = value;
      }

      /// <summary>Returns the folder name (without path) from the last "OpenSingleFolder"-action.</summary>
      /// <returns>Folder name from the last "OpenSingleFolder"-action.</returns>
      public string CurrentOpenSingleFolderName => getNameFromPath(CurrentOpenSingleFolder);

      /// <summary>Returns the array of folders from the last "OpenFolders"-action.</summary>
      /// <returns>Array of folders from the last "OpenFolders"-action.</returns>
      public string[] CurrentOpenFolders
      {
         get => wrapperHolder?.PlatformWrapper?.CurrentOpenFolders;
         set => wrapperHolder.PlatformWrapper.CurrentOpenFolders = value;
      }

      /// <summary>Returns the file from the last "SaveFile"-action.</summary>
      /// <returns>File from the last "SaveFile"-action.</returns>
      public string CurrentSaveFile
      {
         get => wrapperHolder?.PlatformWrapper?.CurrentSaveFile;
         set => wrapperHolder.PlatformWrapper.CurrentSaveFile = value;
      }

      /// <summary>Returns the file name (without path) from the last "SaveFile"-action.</summary>
      /// <returns>File name from the last "SaveFile"-action.</returns>
      public string CurrentSaveFileName => getNameFromPath(CurrentSaveFile);

      /// <summary>Returns the data of the file from the last "OpenSingleFile"-action.</summary>
      /// <returns>Data of the file from the last "OpenSingleFile"-action.</returns>
      public byte[] CurrentOpenSingleFileData => wrapperHolder?.PlatformWrapper?.CurrentOpenSingleFileData;

      /// <summary>The data for the "SaveFile"-action.</summary>
      public byte[] CurrentSaveFileData
      {
         get => wrapperHolder?.PlatformWrapper?.CurrentSaveFileData;
         set => wrapperHolder.PlatformWrapper.CurrentSaveFileData = value;
      }

      #region Wrapper delegates

      /// <summary>Indicates if this wrapper can open a file.</summary>
      /// <returns>Wrapper can open a file.</returns>
      public bool canOpenFile => wrapperHolder?.PlatformWrapper.canOpenFile ?? false;

      /// <summary>Indicates if this wrapper can open a folder.</summary>
      /// <returns>Wrapper can open a folder.</returns>
      public bool canOpenFolder => wrapperHolder?.PlatformWrapper.canOpenFolder ?? false;

      /// <summary>Indicates if this wrapper can save a file.</summary>
      /// <returns>Wrapper can save a file.</returns>
      public bool canSaveFile => wrapperHolder?.PlatformWrapper.canSaveFile ?? false;

      /// <summary>Indicates if this wrapper can open multiple files.</summary>
      /// <returns>Wrapper can open multiple files.</returns>
      public bool canOpenMultipleFiles => wrapperHolder?.PlatformWrapper.canOpenMultipleFiles ?? false;

      /// <summary>Indicates if this wrapper can open multiple folders.</summary>
      /// <returns>Wrapper can open multiple folders.</returns>
      public bool canOpenMultipleFolders => wrapperHolder?.PlatformWrapper.canOpenMultipleFolders ?? false;

      /// <summary>Indicates if this wrapper is supporting the current platform.</summary>
      /// <returns>True if this wrapper supports current platform.</returns>
      public bool isPlatformSupported => wrapperHolder?.PlatformWrapper.isPlatformSupported ?? true;

      /// <summary>Indicates if this wrapper is working directly inside the Unity Editor (without 'Play'-mode).</summary>
      /// <returns>True if this wrapper is working directly inside the Unity Editor.</returns>
      public bool isWorkingInEditor => wrapperHolder?.PlatformWrapper.isWorkingInEditor ?? false;

      #endregion

      #endregion


      #region Events

      [Header("Events")] public OnOpenFilesCompleted OnOpenFilesCompleted;
      public OnOpenFoldersCompleted OnOpenFoldersCompleted;
      public OnSaveFileCompleted OnSaveFileCompleted;

      public delegate void OpenFilesStart();

      public delegate void OpenFilesComplete(bool selected, string singleFile, string[] files);

      public delegate void OpenFoldersStart();

      public delegate void OpenFoldersComplete(bool selected, string singleFolder, string[] folders);

      public delegate void SaveFileStart();

      public delegate void SaveFileComplete(bool selected, string file);

      /// <summary>An event triggered whenever "OpenFiles" is started.</summary>
      public event OpenFilesStart OnOpenFilesStart;

      /// <summary>An event triggered whenever "OpenFiles" is completed.</summary>
      public event OpenFilesComplete OnOpenFilesComplete;

      /// <summary>An event triggered whenever "OpenFolders" is started.</summary>
      public event OpenFoldersStart OnOpenFoldersStart;

      /// <summary>An event triggered whenever "OpenFolders" is completed.</summary>
      public event OpenFoldersComplete OnOpenFoldersComplete;

      /// <summary>An event triggered whenever "SaveFile" is started.</summary>
      public event SaveFileStart OnSaveFileStart;

      /// <summary>An event triggered whenever "SaveFile" is completed.</summary>
      public event SaveFileComplete OnSaveFileComplete;

      #endregion


      #region MonoBehaviour methods

      protected override void Awake()
      {
         base.Awake();

         //if (!Helper.isEditorMode && DontDestroy && CustomMode && CustomWrapper != null)
         //   CustomWrapper.transform.parent = transform;

         wrapperHolder = new WrapperHolder();
      }

      private void Update()
      {
         if (lastOpenFiles != CurrentOpenFiles || lastOpenSingleFile != CurrentOpenSingleFile)
         {
            lastOpenFiles = CurrentOpenFiles;
            lastOpenSingleFile = CurrentOpenSingleFile;
            bool selected = false;
            string singleFile = null;

            if (lastOpenFiles?.Length > 0)
            {
               selected = true;
               singleFile = lastOpenFiles[0];
            }

            onOpenFilesComplete(selected, singleFile, lastOpenFiles);
         }

         if (lastOpenFolders != CurrentOpenFolders || lastOpenSingleFolder != CurrentOpenSingleFolder)
         {
            lastOpenFolders = CurrentOpenFolders;
            lastOpenSingleFolder = CurrentOpenSingleFolder;

            bool selected = false;
            string singleFolder = null;

            if (lastOpenFolders?.Length > 0)
            {
               selected = !string.IsNullOrEmpty(lastOpenFolders[0]);
               singleFolder = lastOpenFolders[0];
            }

            onOpenFoldersComplete(selected, singleFolder, lastOpenFolders);
         }

         if (lastSaveFile != CurrentSaveFile)
         {
            lastSaveFile = CurrentSaveFile;
            bool selected = !string.IsNullOrEmpty(lastSaveFile);

            onSaveFileComplete(selected, lastSaveFile);
         }
      }

      #endregion


      #region Public methods

      /// <summary>Open native file browser for a single file.</summary>
      /// <param name="extension">Allowed extension, e.g. "png" (optional)</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      public string OpenSingleFile(string extension = "*")
      {
         return OpenSingleFile(string.Empty, string.Empty, string.Empty, getFilter(extension));
      }

      /// <summary>Open native file browser for a single file.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      public string OpenSingleFile(string title, string directory, string defaultName, params string[] extensions)
      {
         return OpenSingleFile(title, directory, defaultName, getFilter(extensions));
      }

      /// <summary>Open native file browser for a single file.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      public string OpenSingleFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
         {
            Debug.LogWarning("'defaultName' contains invalid characters - can not show 'OpenSingleFile' dialog!", this);
            return null;
         }

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenSingleFile' dialog!", this);
            return null;
         }

         if (canOpenFile)
         {
            onOpenFilesStart();

            wrapperHolder.PlatformWrapper.OpenSingleFile(string.IsNullOrEmpty(title) ? titleOpenFile : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), defaultName, extensions);

            return CurrentOpenSingleFile;
         }

         Debug.LogWarning("'OpenSingleFile' is currently not supported for the current platform!", this);
         return null;
      }

      /// <summary>Open native file browser for multiple files.</summary>
      /// <param name="extension">Allowed extension, e.g. "png" (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      public string[] OpenFiles(string extension = "*")
      {
         return OpenFiles(string.Empty, string.Empty, string.Empty, getFilter(extension));
      }

      /// <summary>Open native file browser for multiple files.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      public string[] OpenFiles(string title, string directory, string defaultName, params string[] extensions)
      {
         return OpenFiles(title, directory, defaultName, getFilter(extensions));
      }

      /// <summary>Open native file browser for multiple files.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      public string[] OpenFiles(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return null;

         if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
         {
            Debug.LogWarning("'defaultName' contains invalid characters - can not show 'OpenFiles' dialog!", this);
            return null;
         }

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenFiles' dialog!", this);
            return null;
         }

         if (canOpenFile)
         {
            onOpenFilesStart();

            wrapperHolder.PlatformWrapper.OpenFiles(string.IsNullOrEmpty(title) ? titleOpenFiles : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), defaultName, true, extensions);

            return CurrentOpenFiles;
         }

         Debug.LogWarning("'OpenFiles' is currently not supported for the current platform!", this);
         return null;
      }

      /// <summary>Open native folder browser for a single folder.</summary>
      /// <returns>Returns a string of the chosen folder. Empty string when cancelled</returns>
      public string OpenSingleFolder()
      {
         return OpenSingleFolder(string.Empty);
      }

      /// <summary>
      /// Open native folder browser for a single folder.
      /// NOTE: Title is not supported under Windows and UWP (WSA)!
      /// </summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory (default: current, optional)</param>
      /// <returns>Returns a string of the chosen folder. Empty string when cancelled</returns>
      public string OpenSingleFolder(string title, string directory = "")
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenSingleFolder' dialog!", this);
            return null;
         }

         if (canOpenFolder)
         {
            onOpenFoldersStart();

            wrapperHolder.PlatformWrapper.OpenSingleFolder(string.IsNullOrEmpty(title) ? titleOpenFolder : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false));

            return CurrentOpenSingleFolder;
         }

         Debug.LogWarning("'OpenSingleFolder' is currently not supported for the current platform!", this);
         return null;
      }

      /// <summary>
      /// Open native folder browser for multiple folders.
      /// NOTE: Title and multiple folder selection are not supported under Windows and UWP (WSA)!
      /// </summary>
      /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
      public string[] OpenFolders()
      {
         return OpenFolders(string.Empty);
      }

      /// <summary>
      /// Open native folder browser for multiple folders.
      /// NOTE: Title and multiple folder selection are not supported under Windows and UWP (WSA)!
      /// </summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory (default: current, optional)</param>
      /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
      public string[] OpenFolders(string title, string directory = "")
      {
         if (this != null && !isActiveAndEnabled)
            return null;

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenFolders' dialog!", this);
            return null;
         }

         if (canOpenFolder)
         {
            onOpenFoldersStart();

            wrapperHolder.PlatformWrapper.OpenFolders(string.IsNullOrEmpty(title) ? titleOpenFolders : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), true);

            return CurrentOpenFolders;
         }

         Debug.LogWarning("'OpenFolders' is currently not supported for the current platform!", this);
         return null;
      }

      /// <summary>Open native save file browser.</summary>
      /// <param name="defaultName">Default file name (optional)</param>
      /// <param name="extension">File extensions, e.g. "png" (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      public string SaveFile(string defaultName = "", string extension = "*")
      {
         return SaveFile(string.Empty, string.Empty, defaultName, getFilter(extension));
      }

      /// <summary>Open native save file browser.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">File extensions, e.g. "png" (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      public string SaveFile(string title, string directory, string defaultName, params string[] extensions)
      {
         return SaveFile(title, directory, defaultName, getFilter(extensions));
      }

      /// <summary>Open native save file browser</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      public string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
         {
            Debug.LogWarning("'defaultName' contains invalid characters - can not show 'SaveFile' dialog!", this);
            return null;
         }

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'SaveFile' dialog!", this);
            return null;
         }

         if (canSaveFile)
         {
            onSaveFileStart();

            wrapperHolder.PlatformWrapper.SaveFile(string.IsNullOrEmpty(title) ? titleSaveFile : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), string.IsNullOrEmpty(defaultName) ? NameSaveFile : defaultName, extensions);

            return CurrentSaveFile;
         }

         Debug.LogWarning("'SaveFile' is currently not supported for the current platform!", this);
         return null;
      }

      /// <summary>Asynchronously opens native file browser for a single file.</summary>
      /// <param name="extension">Allowed extension, e.g. "png" (optional)</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      public void OpenSingleFileAsync(string extension = "*")
      {
         OpenSingleFileAsync(string.Empty, string.Empty, string.Empty, getFilter(extension));
      }

      /// <summary>Asynchronously opens native file browser for a single file.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      public void OpenSingleFileAsync(string title, string directory, string defaultName, params string[] extensions)
      {
         OpenSingleFileAsync(title, directory, defaultName, getFilter(extensions));
      }

      /// <summary>Asynchronously opens native file browser for a single file.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      public void OpenSingleFileAsync(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
         {
            Debug.LogWarning("'defaultName' contains invalid characters - can not show 'OpenSingleFileAsync' dialog!", this);
            return;
         }

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenSingleFileAsync' dialog!", this);
            return;
         }

         if (canOpenFile)
         {
            onOpenFilesStart();

            wrapperHolder.PlatformWrapper.OpenFilesAsync(string.IsNullOrEmpty(title) ? titleOpenFile : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), defaultName, false, extensions, resetOpenFiles);
         }
         else
         {
            Debug.LogWarning("'OpenSingleFileAsync' is currently not supported for the current platform!", this);
         }
      }

      /// <summary>Asynchronously opens native file browser for multiple files.</summary>
      /// <param name="multiselect">Allow multiple file selection (default: true, optional)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      public void OpenFilesAsync(bool multiselect = true, params string[] extensions)
      {
         OpenFilesAsync(string.Empty, string.Empty, string.Empty, multiselect, getFilter(extensions));
      }

      /// <summary>Asynchronously opens native file browser for multiple files.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection (default: true, optional)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      public void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect = true, params string[] extensions)
      {
         OpenFilesAsync(title, directory, defaultName, multiselect, getFilter(extensions));
      }

      /// <summary>Asynchronously opens native file browser for multiple files.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection (default: true, optional)</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      public void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect = true, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
         {
            Debug.LogWarning("'defaultName' contains invalid characters - can not show 'OpenFilesAsync' dialog!", this);
            return;
         }

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenFilesAsync' dialog!", this);
            return;
         }

         if (canOpenFile)
         {
            onOpenFilesStart();

            wrapperHolder.PlatformWrapper.OpenFilesAsync(string.IsNullOrEmpty(title) ? multiselect ? titleOpenFiles : titleOpenFile : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), defaultName, multiselect, extensions, resetOpenFiles);
         }
         else
         {
            Debug.LogWarning("'OpenFilesAsync' is currently not supported for the current platform!", this);
         }
      }

      /// <summary>Asynchronously opens native folder browser for a single folder.</summary>
      /// <returns>Returns a string of the chosen folder. Empty string when cancelled</returns>
      public void OpenSingleFolderAsync()
      {
         OpenSingleFolderAsync(string.Empty);
      }

      /// <summary>
      /// Asynchronously opens native folder browser for a single folder.
      /// NOTE: Title is not supported under Windows and UWP (WSA)!
      /// </summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory (default: current, optional)</param>
      /// <returns>Returns a string of the chosen folder. Empty string when cancelled</returns>
      public void OpenSingleFolderAsync(string title, string directory = "")
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenSingleFolderAsync' dialog!", this);
            return;
         }

         if (canOpenFolder)
         {
            onOpenFoldersStart();

            wrapperHolder.PlatformWrapper.OpenFoldersAsync(string.IsNullOrEmpty(title) ? titleOpenFolder : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), false, resetOpenFolders);
         }
         else
         {
            Debug.LogWarning("'OpenSingleFolderAsync' is currently not supported for the current platform!", this);
         }
      }

      /// <summary>Asynchronously opens native folder browser for multiple folders.</summary>
      /// <param name="multiselect">Allow multiple folder selection (default: true, optional)</param>
      /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
      public void OpenFoldersAsync(bool multiselect = true)
      {
         OpenFoldersAsync(string.Empty, string.Empty, multiselect);
      }

      /// <summary>Asynchronously opens native folder browser for multiple folders.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory (default: current, optional)</param>
      /// <param name="multiselect">Allow multiple folder selection (default: true, optional)</param>
      /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
      public void OpenFoldersAsync(string title, string directory = "", bool multiselect = true)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenFoldersAsync' dialog!", this);
            return;
         }

         if (canOpenFolder)
         {
            onOpenFoldersStart();

            wrapperHolder.PlatformWrapper.OpenFoldersAsync(string.IsNullOrEmpty(title) ? multiselect ? titleOpenFolders : titleOpenFolder : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), multiselect, resetOpenFolders);
         }
         else
         {
            Debug.LogWarning("'OpenFoldersAsync' is currently not supported for the current platform!", this);
         }
      }

      /// <summary>Asynchronously opens native save file browser.</summary>
      /// <param name="defaultName">Default file name (optional)</param>
      /// <param name="extension">File extension, e.g. "png" (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      public void SaveFileAsync(string defaultName = "", string extension = "*")
      {
         SaveFileAsync(string.Empty, string.Empty, defaultName, getFilter(extension));
      }

      /// <summary>Asynchronously opens native save file browser.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">File extensions, e.g. "png" (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      public void SaveFileAsync(string title, string directory, string defaultName, params string[] extensions)
      {
         SaveFileAsync(title, directory, defaultName, getFilter(extensions));
      }

      /// <summary>Asynchronously opens native save file browser (async)</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      public void SaveFileAsync(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
         {
            Debug.LogWarning("'defaultName' contains invalid characters - can not show 'SaveFileAsync' dialog!", this);
            return;
         }

         if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
         {
            Debug.LogWarning("'directory' contains invalid characters - can not show 'SaveFileAsync' dialog!", this);
            return;
         }

         if (canSaveFile)
         {
            onSaveFileStart();

            wrapperHolder.PlatformWrapper.SaveFileAsync(string.IsNullOrEmpty(title) ? titleSaveFile : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), string.IsNullOrEmpty(defaultName) ? NameSaveFile : defaultName, extensions, paths => resetSaveFile(paths));
         }
         else
         {
            Debug.LogWarning("'SaveFileAsync' is currently not supported for the current platform!", this);
         }
      }

      /// <summary>Find files inside a path.</summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="extensions">Extensions for the file search, e.g. "png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public string[] GetFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         return Crosstales.Common.Util.FileHelper.GetFiles(path, isRecursive, extensions);
      }

      /// <summary>Find files inside a path.</summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search</param>
      /// <param name="extensions">List of extension filters for the search (optional)</param>
      /// <returns>Returns array of the found files inside the path. Zero length array when an error occured.</returns>
      public string[] GetFiles(string path, bool isRecursive, params ExtensionFilter[] extensions)
      {
         return GetFiles(path, isRecursive, extensions.SelectMany(extensionFilter => extensionFilter.Extensions).ToArray());
      }

      /// <summary>Find folders inside.</summary>
      /// <param name="path">Path to find the directories</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <returns>Returns array of the found directories inside the path. Zero length array when an error occured.</returns>
      public string[] GetFolders(string path, bool isRecursive = false)
      {
         return Crosstales.Common.Util.FileHelper.GetDirectories(path, isRecursive);
      }

      /// <summary>
      /// Find all logical drives.
      /// </summary>
      /// <returns>Returns array of the found drives. Zero length array when an error occured.</returns>
      public string[] GetDrives()
      {
         return Crosstales.Common.Util.FileHelper.GetDrives();
      }

      /// <summary>Copy or move a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <param name="destFile">Destination file path</param>
      /// <param name="move">Move file instead of copy (default: false, optional)</param>
      public static void CopyFile(string sourceFile, string destFile, bool move = false)
      {
         Crosstales.Common.Util.FileHelper.CopyFile(sourceFile, destFile, move);
      }

      /// <summary>Copy or move a folder.</summary>
      /// <param name="sourcePath">Source folder path</param>
      /// <param name="destPath">Destination folder path</param>
      /// <param name="move">Move folder instead of copy (default: false, optional)</param>
      public static void CopyFolder(string sourcePath, string destPath, bool move = false)
      {
         Crosstales.Common.Util.FileHelper.CopyPath(sourcePath, destPath, move);
      }

      /// <summary>
      /// Shows the location of a file (or folder) in OS file explorer.
      /// NOTE: only works on standalone platforms
      /// </summary>
      public static void ShowFile(string file)
      {
         Crosstales.Common.Util.FileHelper.ShowFile(file);
      }

      /// <summary>
      /// Shows the location of a folder (or file) in OS file explorer.
      /// NOTE: only works on standalone platforms
      /// </summary>
      public static void ShowFolder(string path)
      {
         Crosstales.Common.Util.FileHelper.ShowPath(path);
      }

      /// <summary>
      /// Opens a file with the OS default application.
      /// NOTE: only works for standalone platforms
      /// </summary>
      /// <param name="file">File path</param>
      public static void OpenFile(string file)
      {
         Crosstales.Common.Util.FileHelper.OpenFile(file);
      }


      #region Legacy

      /// <summary>Open native file browser for multiple files.</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="multiselect">Allow multiple file selection (default: true, optional)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void OpenFilesAsync(System.Action<string[]> cb, bool multiselect = true, params string[] extensions)
      {
         OpenFilesAsync(cb, multiselect ? titleOpenFiles : titleOpenFile, string.Empty, string.Empty, multiselect, getFilter(extensions));
      }

      /// <summary>Open native file browser for multiple files.</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection (default: true, optional)</param>
      /// <param name="extensions">Allowed extensions, e.g. "png" (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void OpenFilesAsync(System.Action<string[]> cb, string title, string directory, string defaultName, bool multiselect = true, params string[] extensions)
      {
         OpenFilesAsync(cb, title, directory, defaultName, multiselect, getFilter(extensions));
      }

      /// <summary>Open native file browser for multiple files (async).</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection (default: true, optional)</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void OpenFilesAsync(System.Action<string[]> cb, string title, string directory, string defaultName, bool multiselect = true, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (cb != null)
         {
            if (canOpenFile)
            {
               if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
               {
                  Debug.LogWarning("'defaultName' contains invalid characters - can not show 'OpenFiles' dialog!", this);
                  return;
               }

               if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
               {
                  Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenFiles' dialog!", this);
                  return;
               }

               wrapperHolder.PlatformWrapper.OpenFilesAsync(string.IsNullOrEmpty(title) ? titleOpenFiles : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), defaultName, multiselect, extensions, cb);
            }
            else
            {
               Debug.LogWarning("'OpenFilesAsync' is currently not supported for the current platform!", this);
            }
         }
         else
         {
            Debug.LogWarning("'cb'  (callback)is null - can not show dialog!", this);
         }
      }

      /// <summary>Open native folder browser for multiple folders (async).</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="multiselect">Allow multiple folder selection (default: true, optional)</param>
      /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void OpenFoldersAsync(System.Action<string[]> cb, bool multiselect = true)
      {
         OpenFoldersAsync(cb, titleOpenFolders, string.Empty, multiselect);
      }

      /// <summary>Open native folder browser for multiple folders (async).</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory (default: current, optional)</param>
      /// <param name="multiselect">Allow multiple folder selection (default: true, optional)</param>
      /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void OpenFoldersAsync(System.Action<string[]> cb, string title, string directory = "", bool multiselect = true)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (cb != null)
         {
            if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
            {
               Debug.LogWarning("'directory' contains invalid characters - can not show 'OpenFolders' dialog!", this);
               return;
            }

            if (canOpenFolder)
            {
               wrapperHolder.PlatformWrapper.OpenFoldersAsync(string.IsNullOrEmpty(title) ? titleOpenFolders : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), multiselect, cb);
            }
            else
            {
               Debug.LogWarning("'OpenFoldersAsync' is currently not supported for the current platform!", this);
            }
         }
         else
         {
            Debug.LogWarning("'cb' (callback) is null - can not show dialog!", this);
         }
      }

      /// <summary>Open native save file browser</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="defaultName">Default file name (optional)</param>
      /// <param name="extension">File extension, e.g. "png" (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void SaveFileAsync(System.Action<string> cb, string defaultName = "", string extension = "*")
      {
         SaveFileAsync(cb, titleSaveFile, string.Empty, defaultName, getFilter(extension));
      }

      /// <summary>Open native save file browser</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">File extensions, e.g. "png" (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void SaveFileAsync(System.Action<string> cb, string title, string directory, string defaultName, params string[] extensions)
      {
         SaveFileAsync(cb, title, directory, defaultName, getFilter(extensions));
      }

      /// <summary>Open native save file browser (async).</summary>
      /// <param name="cb">Callback for the async operation.</param>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters (optional)</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      //[System.Obsolete("This method is deprecated, please use it without the callback.")]
      public void SaveFileAsync(System.Action<string> cb, string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (cb != null)
         {
            if (Crosstales.Common.Util.FileHelper.FileHasInvalidChars(defaultName))
            {
               Debug.LogWarning("'defaultName' contains invalid characters - can not show 'SaveFileAsync' dialog!", this);
               return;
            }

            if (Crosstales.Common.Util.FileHelper.PathHasInvalidChars(directory))
            {
               Debug.LogWarning("'directory' contains invalid characters - can not show 'SaveFileAsync' dialog!", this);
               return;
            }

            if (canSaveFile)
            {
               wrapperHolder.PlatformWrapper.SaveFileAsync(string.IsNullOrEmpty(title) ? titleSaveFile : title, Crosstales.Common.Util.FileHelper.ValidatePath(directory, true, false), string.IsNullOrEmpty(defaultName) ? NameSaveFile : defaultName, extensions, cb);
            }
            else
            {
               Debug.LogWarning("'SaveFileAsync' is currently not supported for the current platform!", this);
            }
         }
         else
         {
            Debug.LogWarning("'cb' (callback) is null - can not show dialog!", this);
         }
      }

      #endregion

      #endregion


      #region Private methods

      private string getNameFromPath(string path) //TODO move to BaseHelper?
      {
         if (string.IsNullOrEmpty(path))
            return null;

         int nameIndex = path.LastIndexOf("/");

         if (nameIndex < 0)
            nameIndex = path.LastIndexOf(@"\");

         if (nameIndex < 0)
            nameIndex = -1;

         return path.Substring(nameIndex + 1);
      }

      private void resetOpenFiles(params string[] paths)
      {
         //do nothing
         //lastOpenFiles = System.Array.Empty<string>();
         //lastOpenSingleFile = string.Empty;
      }

      private void resetOpenFolders(params string[] paths)
      {
         //do nothing
         //lastOpenFolders = System.Array.Empty<string>();
         //lastOpenSingleFolder = string.Empty;
      }

      private void resetSaveFile(params string[] paths)
      {
         //do nothing
         //lastSaveFile = string.Empty;
      }

      private ExtensionFilter[] getFilter(params string[] extensions)
      {
         if (extensions?.Length > 0)
         {
            if (extensions.Length == 1 && "*".Equals(extensions[0]))
               return null;

            ExtensionFilter[] filter = new ExtensionFilter[extensions.Length];

            for (int ii = 0; ii < extensions.Length; ii++)
            {
               string extension = string.IsNullOrEmpty(extensions[ii]) ? "*" : extensions[ii];

               if (extension.Equals("*"))
               {
                  filter[ii] = new ExtensionFilter(TextAllFiles, Helper.isMacOSEditor ? string.Empty : extension);
               }
               else
               {
                  filter[ii] = new ExtensionFilter(extension, extension);
               }
            }

            if (Config.DEBUG)
               Debug.Log($"getFilter: {filter.CTDump()}", this);

            return filter;
         }

         return null;
      }

      #endregion


      #region Event-trigger methods

      private void onOpenFilesStart()
      {
         if (Config.DEBUG)
            Debug.Log("onOpenFilesStart", this);

         lastOpenFiles = CurrentOpenFiles = null;
         lastOpenSingleFile = CurrentOpenSingleFile = null;

         OnOpenFilesStart?.Invoke();
      }

      private void onOpenFilesComplete(bool selected, string singleFile, string[] files)
      {
         if (Config.DEBUG)
            Debug.Log($"onOpenFilesComplete: {selected} - {singleFile} - {CurrentOpenSingleFileData?.Length}", this);

         if (!Helper.isEditorMode)
         {
            string fileList = files?.Length > 0 ? string.Join(";", files) : null;

            OnOpenFilesCompleted?.Invoke(selected, singleFile, fileList);
         }

         OnOpenFilesComplete?.Invoke(selected, singleFile, files);
      }

      private void onOpenFoldersStart()
      {
         if (Config.DEBUG)
            Debug.Log("onOpenFoldersStart", this);

         lastOpenFolders = CurrentOpenFolders = null;
         lastOpenSingleFolder = CurrentOpenSingleFolder = null;

         OnOpenFoldersStart?.Invoke();
      }

      private void onOpenFoldersComplete(bool selected, string singleFolder, string[] folders)
      {
         if (Config.DEBUG)
            Debug.Log($"onOpenFoldersComplete: {selected} - {singleFolder}", this);

         if (!Helper.isEditorMode)
         {
            string folderList = folders?.Length > 0 ? string.Join(";", folders) : null;

            OnOpenFoldersCompleted?.Invoke(selected, singleFolder, folderList);
         }

         OnOpenFoldersComplete?.Invoke(selected, singleFolder, folders);
      }

      private void onSaveFileStart()
      {
         if (Config.DEBUG)
            Debug.Log("onSaveFileStart", this);

         lastSaveFile = CurrentSaveFile = null;

         OnSaveFileStart?.Invoke();
      }

      private void onSaveFileComplete(bool selected, string file)
      {
         if (Config.DEBUG)
            Debug.Log($"onSaveFileComplete: {selected} - {file} - {CurrentSaveFileData?.Length}", this);

         if (selected && (!Helper.isWebPlatform || Helper.isEditor) && CurrentSaveFileData != null)
         {
            try
            {
               System.IO.File.WriteAllBytes(file, CurrentSaveFileData);
            }
            catch (System.Exception ex)
            {
               //if (Config.DEBUG)
               Debug.LogWarning($"Could not write file: {file} - {ex}", this);
            }
         }

         if (!Helper.isEditorMode)
            OnSaveFileCompleted?.Invoke(selected, file);

         OnSaveFileComplete?.Invoke(selected, file);
      }

      #endregion
   }

   /// <summary>Filter for extensions.</summary>
   public struct ExtensionFilter
   {
      public string Name;
      public string[] Extensions;

      public ExtensionFilter(string filterName, params string[] filterExtensions)
      {
         Name = filterName;
         Extensions = filterExtensions;
      }

      public override string ToString()
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();

         result.Append(GetType().Name);
         result.Append(Constants.TEXT_TOSTRING_START);

         result.Append("Name='");
         result.Append(Name);
         result.Append(Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Extensions='");
         result.Append(Extensions.CTDump());
         result.Append(Constants.TEXT_TOSTRING_DELIMITER_END);

         result.Append(Constants.TEXT_TOSTRING_END);

         return result.ToString();
      }
   }

   [System.Serializable]
   public class OnOpenFilesCompleted : UnityEngine.Events.UnityEvent<bool, string, string>
   {
   }

   [System.Serializable]
   public class OnOpenFoldersCompleted : UnityEngine.Events.UnityEvent<bool, string, string>
   {
   }

   [System.Serializable]
   public class OnSaveFileCompleted : UnityEngine.Events.UnityEvent<bool, string>
   {
   }

   internal class WrapperHolder
   {
      #region Variables

      public IFileBrowser PlatformWrapper { get; private set; }

      #endregion


      #region Constructor

      public WrapperHolder()
      {
         bool useCustom = FileBrowser.Instance.CustomWrapper != null && FileBrowser.Instance.CustomMode && FileBrowser.Instance.CustomWrapper.enabled;

         if (useCustom)
         {
            PlatformWrapper = FileBrowser.Instance.CustomWrapper;
         }
         else
         {
//#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#if UNITY_EDITOR_WIN
            if (Helper.isEditor && !Config.NATIVE_WINDOWS)
#else
            if (Helper.isEditor)
#endif
            {
#if UNITY_EDITOR
               PlatformWrapper = new FileBrowserEditor();
#endif
            }
            else if (Helper.isMacOSPlatform)
            {
#if UNITY_STANDALONE_OSX || CT_DEVELOP
               PlatformWrapper = new FileBrowserMac();
#endif
            }
            else if (Helper.isWindowsPlatform || Helper.isWindowsEditor)
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
               PlatformWrapper = new FileBrowserWindows();
#endif
            }
            else if (Helper.isLinuxPlatform)
            {
#if UNITY_STANDALONE_LINUX
            PlatformWrapper = new FileBrowserLinux();
#endif
            }
            else if (Helper.isWSAPlatform)
            {
#if UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
               PlatformWrapper = new FileBrowserWSA();
#endif
            }
            else
            {
               PlatformWrapper = new FileBrowserGeneric();
            }
         }

         if (Config.DEBUG)
            Debug.Log(PlatformWrapper);
      }

      #endregion
   }
}
// © 2017-2023 crosstales LLC (https://www.crosstales.com)