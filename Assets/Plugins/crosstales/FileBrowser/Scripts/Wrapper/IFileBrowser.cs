namespace Crosstales.FB.Wrapper
{
   /// <summary>Interface for all file browsers.</summary>
   public interface IFileBrowser
   {
      #region Properties

      /// <summary>Indicates if this wrapper can open a file.</summary>
      /// <returns>Wrapper can open a file.</returns>
      bool canOpenFile { get; }

      /// <summary>Indicates if this wrapper can open a folder.</summary>
      /// <returns>Wrapper can open a folder.</returns>
      bool canOpenFolder { get; }

      /// <summary>Indicates if this wrapper can save a file.</summary>
      /// <returns>Wrapper can save a file.</returns>
      bool canSaveFile { get; }

      /// <summary>Indicates if this wrapper can open multiple files.</summary>
      /// <returns>Wrapper can open multiple files.</returns>
      bool canOpenMultipleFiles { get; }

      /// <summary>Indicates if this wrapper can open multiple folders.</summary>
      /// <returns>Wrapper can open multiple folders.</returns>
      bool canOpenMultipleFolders { get; }

      /// <summary>Indicates if this wrapper is supporting the current platform.</summary>
      /// <returns>True if this wrapper supports current platform.</returns>
      bool isPlatformSupported { get; }

      /// <summary>Indicates if this wrapper is working directly inside the Unity Editor (without 'Play'-mode).</summary>
      /// <returns>True if this wrapper is working directly inside the Unity Editor.</returns>
      bool isWorkingInEditor { get; }

      /// <summary>Returns the file from the last "OpenSingleFile"-action.</summary>
      /// <returns>File from the last "OpenSingleFile"-action.</returns>
      string CurrentOpenSingleFile { get; set; }

      /// <summary>Returns the array of files from the last "OpenFiles"-action.</summary>
      /// <returns>Array of files from the last "OpenFiles"-action.</returns>
      string[] CurrentOpenFiles { get; set; }

      /// <summary>Returns the folder from the last "OpenSingleFolder"-action.</summary>
      /// <returns>Folder from the last "OpenSingleFolder"-action.</returns>
      string CurrentOpenSingleFolder { get; set; }

      /// <summary>Returns the array of folders from the last "OpenFolders"-action.</summary>
      /// <returns>Array of folders from the last "OpenFolders"-action.</returns>
      string[] CurrentOpenFolders { get; set; }

      /// <summary>Returns the file from the last "SaveFile"-action.</summary>
      /// <returns>File from the last "SaveFile"-action.</returns>
      string CurrentSaveFile { get; set; }

      /// <summary>Returns the data of the file from the last "OpenSingleFile"-action.</summary>
      /// <returns>Data of the file from the last "OpenSingleFile"-action.</returns>
      byte[] CurrentOpenSingleFileData { get; }

      /// <summary>The data for the "SaveFile"-action.</summary>
      byte[] CurrentSaveFileData { get; set; }

      #endregion


      #region Methods

      /// <summary>Open native file browser for a single file.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <returns>Returns a string of the chosen file. Null when cancelled</returns>
      string OpenSingleFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions);

      /// <summary>Open native file browser for multiple files.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <returns>Returns array of chosen files. Null when cancelled</returns>
      string[] OpenFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions);

      /// <summary>Open native folder browser for a single folder.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <returns>Returns a string of the chosen folder. Null when cancelled</returns>
      string OpenSingleFolder(string title, string directory);

      /// <summary>Open native folder browser for multiple folders.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="multiselect">Allow multiple folder selection</param>
      /// <returns>Returns array of chosen folders. Null when cancelled</returns>
      string[] OpenFolders(string title, string directory, bool multiselect);

      /// <summary>Open native save file browser.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <returns>Returns chosen file. Null when cancelled</returns>
      string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions);

      /// <summary>Asynchronously opens native file browser for multiple files.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <param name="cb">Callback for the async operation.</param>
      /// <returns>Returns array of chosen files. Null when cancelled</returns>
      void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, System.Action<string[]> cb);

      /// <summary>Asynchronously opens native folder browser for multiple folders.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="multiselect">Allow multiple folder selection</param>
      /// <param name="cb">Callback for the async operation.</param>
      /// <returns>Returns array of chosen folders. Null when cancelled</returns>
      void OpenFoldersAsync(string title, string directory, bool multiselect, System.Action<string[]> cb);

      /// <summary>Asynchronously opens native save file browser.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <param name="cb">Callback for the async operation.</param>
      /// <returns>Returns chosen file. Null when cancelled</returns>
      void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb);

      //TODO add NEW methods
/*
      /// <summary>Open native file browser for a single file and reads the data.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
      string OpenAndReadSingleFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions);

      /// <summary>Open native file browser for multiple files and reads the data.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      string[] OpenAndReadFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions);

      /// <summary>Open native save file browser and writes the data.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      string SaveAndWriteFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions);

      /// <summary>Asynchronously opens native file browser for multiple files and reads the data.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name (currently only supported under Windows standalone)</param>
      /// <param name="multiselect">Allow multiple file selection</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <param name="cb">Callback for the async operation.</param>
      /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
      void OpenAndReadFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, System.Action<string[]> cb);

      /// <summary>Asynchronously opens native save file browser and writes the data.</summary>
      /// <param name="title">Dialog title</param>
      /// <param name="directory">Root directory</param>
      /// <param name="defaultName">Default file name</param>
      /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
      /// <param name="cb">Callback for the async operation.</param>
      /// <returns>Returns chosen file. Empty string when cancelled</returns>
      void SaveAndWriteFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb);
*/

      #endregion
   }
}
// © 2018-2023 crosstales LLC (https://www.crosstales.com)