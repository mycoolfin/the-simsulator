#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine;
using System;

namespace Crosstales.FB.Wrapper
{
   /// <summary>File browser implementation for Windows.</summary>
   public class FileBrowserWindows : BaseFileBrowserStandalone
   {
      #region Variables

      private static FileBrowserWindows instance;

      private static string _initialPath = string.Empty;

      private const int OFN_EXPLORER = 0x80000;

      //private const int MAX_FILE_LENGTH = 4096;
      private const int MAX_FILE_LENGTH = 260;

      //private const int MAX_PATH_LENGTH = 4096;
      private const int MAX_PATH_LENGTH = 260;

      private const int WM_USER = 0x400;
      private const int BFFM_INITIALIZED = 1;
      private const int BFFM_SELCHANGED = 2;
      private const int BFFM_SETSELECTIONW = WM_USER + 103;
      private const int BFFM_SETSTATUSTEXTW = WM_USER + 104;

      private const uint BIF_NEWDIALOGSTYLE = 0x0040; // Use the new dialog layout with the ability to resize
      private const uint BIF_SHAREABLE = 0x8000; // sharable resources displayed (remote shares, requires BIF_USENEWUI)

      private static readonly IntPtr currentWindow = NativeMethods.GetActiveWindow();
      private static readonly char[] nullChar = { (char)0 };

      private string allFilesText;

      //private static bool isFirsttime = true; //hacky solution for async and IL2CPP

      #endregion


      #region Properties

      //private static int MAX_OPEN_FILE_LENGTH => 65536;
      private static int MAX_OPEN_FILE_LENGTH => MAX_FILE_LENGTH * Crosstales.FB.Util.Constants.WINDOWS_MIN_OPEN_NUMBER_OF_FILES;

      #endregion


      #region Constructor

      public FileBrowserWindows()
      {
         instance = this;
         allFilesText = FileBrowser.Instance.TextAllFiles;
      }

      #endregion


      #region Implemented methods

      public override bool canOpenMultipleFolders => false;

      public override bool isPlatformSupported => Crosstales.FB.Util.Helper.isWindowsPlatform; // || Util.Helper.isWindowsEditor;

      public override bool isWorkingInEditor => true;

      public override string[] OpenFiles(string title, string directory, string defaultName, bool multiselect, params ExtensionFilter[] extensions)
      {
         //resetOpenFiles();

         return openFiles(title, directory, defaultName, multiselect, false, extensions);
      }

      public override string[] OpenFolders(string title, string directory, bool multiselect)
      {
         if (Crosstales.FB.Util.Config.DEBUG && !string.IsNullOrEmpty(title))
            Debug.LogWarning("'title' is not supported under Windows.");

         if (multiselect)
            Debug.LogWarning("'multiselect' for folders is not supported under Windows.");

         if (!Crosstales.FB.Util.Helper.isIL2CPP && !FileBrowser.Instance.LegacyFolderBrowser)
            Debug.LogWarning("The new folder dialog works only with IL2CPP!");

         //resetOpenFolders();

         string[] paths = null;

         if (!Crosstales.FB.Util.Helper.isIL2CPP || FileBrowser.Instance.LegacyFolderBrowser)
         {
            paths = openFolders(title, directory, false, false);
         }
         else
         {
            paths = openFoldersNew(title, directory, false, false);
         }

         return paths;
      }

      public override string SaveFile(string title, string directory, string defaultName, params ExtensionFilter[] extensions)
      {
         //resetSaveFile();

         return saveFile(title, directory, defaultName, false, extensions);
      }

      public override void OpenFilesAsync(string title, string directory, string defaultName, bool multiselect, ExtensionFilter[] extensions, Action<string[]> cb)
      {
         //resetOpenFiles();

         new System.Threading.Thread(() => cb.Invoke(OpenFiles(title, directory, defaultName, multiselect, extensions))).Start();
      }

      public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
      {
         if (Crosstales.FB.Util.Config.DEBUG && !string.IsNullOrEmpty(title))
            Debug.LogWarning("'title' is not supported under Windows.");

         if (multiselect)
            Debug.LogWarning("'multiselect' for folders is not supported under Windows.");

         if (!Crosstales.FB.Util.Helper.isIL2CPP && !FileBrowser.Instance.LegacyFolderBrowser)
            Debug.LogWarning("The new folder dialog works only with IL2CPP!");

         //resetOpenFolders();

         if (!Crosstales.FB.Util.Helper.isIL2CPP || FileBrowser.Instance.LegacyFolderBrowser) // || isFirsttime)
         {
            new System.Threading.Thread(() => cb.Invoke(openFolders(title, directory, false, true))).Start();
         }
         else
         {
            new System.Threading.Thread(() => cb.Invoke(openFoldersNew(title, directory, false, true))).Start();
         }
      }

      public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
      {
         //resetSaveFile();

         new System.Threading.Thread(() => cb.Invoke(SaveFile(title, directory, defaultName, extensions))).Start();
      }

      #endregion


      #region Private methods

      private static string[] openFiles(string title, string directory, string defaultName, bool multiselect, bool isAsync, params ExtensionFilter[] extensions)
      {
         try
         {
            NativeMethods.OpenFileName ofn = new NativeMethods.OpenFileName();

            string fileName = defaultName ?? "";

            ofn.dlgOwner = currentWindow;
            ofn.filter = getFilterFromFileExtensionList(extensions);
            ofn.filterIndex = 1;

            if (!string.IsNullOrEmpty(directory))
               ofn.initialDir = directory;

            if (multiselect)
            {
               //ofn.file = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni(Util.Helper.CreateString(" ", MAX_OPEN_FILE_LENGTH));
               ofn.file = System.Runtime.InteropServices.Marshal.StringToBSTR(fileName + Crosstales.FB.Util.Helper.CreateString(" ", MAX_OPEN_FILE_LENGTH - fileName.Length));
               ofn.maxFile = MAX_OPEN_FILE_LENGTH;
            }
            else
            {
               ofn.file = System.Runtime.InteropServices.Marshal.StringToBSTR(fileName + Crosstales.FB.Util.Helper.CreateString(" ", MAX_FILE_LENGTH - fileName.Length));
               ofn.maxFile = MAX_FILE_LENGTH;
            }

            ofn.title = title;
            ofn.flags = (int)FOS.FOS_NOCHANGEDIR | (int)FOS.FOS_FILEMUSTEXIST | (int)FOS.FOS_PATHMUSTEXIST | (multiselect ? (int)FOS.FOS_ALLOWMULTISELECT | OFN_EXPLORER : 0x00000000);

            ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);

            //isFirsttime = false;

            if (NativeMethods.GetOpenFileName(ref ofn))
            {
               //string file = System.Runtime.InteropServices.Marshal.PtrToStringUni(ofn.file);
               string file = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ofn.file);

               if (string.IsNullOrEmpty(file))
               {
                  Debug.LogWarning("NO FILE SELECTED!");
                  instance.CurrentOpenFiles = System.Array.Empty<string>();
                  instance.CurrentOpenSingleFile = string.Empty;

                  return null;
               }

               if (multiselect)
               {
                  string[] files = file.Split(nullChar, StringSplitOptions.RemoveEmptyEntries);

                  if (files.Length > 2)
                  {
                     System.Collections.Generic.List<string> selectedFilesList = new System.Collections.Generic.List<string>();

                     for (int ii = 1; ii < files.Length - 1; ii++)
                     {
                        string resultFile = Crosstales.Common.Util.FileHelper.ValidateFile(files[0] + '\\' + files[ii]);
                        selectedFilesList.Add(resultFile);
                     }

                     instance.CurrentOpenFiles = selectedFilesList.ToArray();
                     instance.CurrentOpenSingleFile = instance.CurrentOpenFiles[0];

                     return instance.CurrentOpenFiles;
                  }
               }

               instance.CurrentOpenSingleFile = Crosstales.Common.Util.FileHelper.ValidateFile(file);
               instance.CurrentOpenFiles = new[] { instance.CurrentOpenSingleFile };

               return instance.CurrentOpenFiles;
            }

            instance.CurrentOpenFiles = System.Array.Empty<string>();
            instance.CurrentOpenSingleFile = string.Empty;
         }
         catch (Exception ex)
         {
            instance.CurrentOpenFiles = System.Array.Empty<string>();
            instance.CurrentOpenSingleFile = string.Empty;

            Debug.LogError($"Open file dialog threw an error: {ex}");
         }

         return null;
      }

      private static string[] openFolders(string title, string directory, bool multiselect, bool isAsync)
      {
         if (!string.IsNullOrEmpty(directory))
            _initialPath = directory;

         IntPtr pidl = IntPtr.Zero;
         IntPtr bufferAddress = IntPtr.Zero;

         string path = null;

         try
         {
            NativeMethods.BROWSEINFO bi = new NativeMethods.BROWSEINFO();

            bufferAddress = System.Runtime.InteropServices.Marshal.AllocHGlobal(MAX_PATH_LENGTH);

            bi.dlgOwner = currentWindow;
            bi.pidlRoot = IntPtr.Zero;
            if (isAsync)
            {
               bi.ulFlags = BIF_SHAREABLE;
            }
            else
            {
               bi.ulFlags = BIF_NEWDIALOGSTYLE | BIF_SHAREABLE;
            }

            bi.lpfn = onBrowseEvent;
            bi.lParam = IntPtr.Zero;
            bi.iImage = 0;

            //isFirsttime = false;

            pidl = NativeMethods.SHBrowseForFolder(ref bi);

            if (NativeMethods.SHGetPathFromIDList(pidl, bufferAddress))
            {
               path = System.Runtime.InteropServices.Marshal.PtrToStringUni(bufferAddress);
               _initialPath = path;
            }
         }
         catch (Exception ex)
         {
            Debug.LogError($"Folder dialog threw an error: {ex}");
         }
         finally
         {
            if (bufferAddress != IntPtr.Zero)
               System.Runtime.InteropServices.Marshal.FreeHGlobal(bufferAddress);

            if (pidl != IntPtr.Zero)
               System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pidl);
         }

         if (string.IsNullOrEmpty(path))
         {
            instance.CurrentOpenFolders = System.Array.Empty<string>();
            instance.CurrentOpenSingleFolder = string.Empty;

            return null;
         }

         instance.CurrentOpenFolders = new[] { path };
         instance.CurrentOpenSingleFolder = path;

         return instance.CurrentOpenFolders;
      }

      private static string[] openFoldersNew(string title, string directory, bool multiselect, bool isAsync)
      {
         if (Crosstales.FB.Util.Config.DEBUG && !string.IsNullOrEmpty(directory))
            Debug.LogWarning("'directory' is not supported under Windows.");

         NativeMethods.IFileOpenDialog folderDialog = null;

         try
         {
            folderDialog = (NativeMethods.IFileOpenDialog)new NativeMethods.FileOpenDialog();
            folderDialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
            //dialog.SetOptions(FOS.FOS_PICKFOLDERS);
/*
         NativeMethods.IShellItem item;
         
         if (!string.IsNullOrEmpty(DirectoryPath))
         {
            IntPtr idl = IntPtr.Zero;
            uint atts = 0;
            if (SHILCreateFromPath(DirectoryPath, out idl, ref atts) == 0)
            {
               if (SHCreateShellItem(System.IntPtr.Zero, System.IntPtr.Zero, idl, out item) == 0)
               {
                  dialog.SetFolder(item); //crashes Unity!
               }
            }

            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(idl);
         }
*/
            //isFirsttime = false;

            uint hr = folderDialog.Show(currentWindow);

            if (hr == 0)
            {
               folderDialog.GetResult(out NativeMethods.IShellItem item);

               item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);

               if (string.IsNullOrEmpty(path))
               {
                  instance.CurrentOpenFolders = System.Array.Empty<string>();
                  instance.CurrentOpenSingleFolder = string.Empty;

                  return null;
               }

               instance.CurrentOpenFolders = new[] { path };
               instance.CurrentOpenSingleFolder = path;

               return instance.CurrentOpenFolders;
            }
         }
         catch (Exception ex)
         {
            Debug.LogError($"New folder dialog threw an error: {ex}");

            return openFolders(title, directory, multiselect, isAsync);
         }
         finally
         {
            if (folderDialog != null)
               System.Runtime.InteropServices.Marshal.ReleaseComObject(folderDialog);
         }

         //instance.CurrentOpenFolders = System.Array.Empty<string>();
         //instance.CurrentOpenSingleFolder = string.Empty;

         return null;
      }

      private static string saveFile(string title, string directory, string defaultName, bool isAsync, params ExtensionFilter[] extensions)
      {
         try
         {
            NativeMethods.OpenFileName sfn = new NativeMethods.OpenFileName();
            string defaultExtension = getDefaultExtension(extensions);
            string fileName = defaultExtension.Equals("*") ? defaultExtension : $"{defaultName}.{defaultExtension}";
            sfn.dlgOwner = currentWindow;
            sfn.filter = getFilterFromFileExtensionList(extensions);
            sfn.filterIndex = 1;
            if (!string.IsNullOrEmpty(directory))
               sfn.initialDir = directory;

            //sfn.file = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni(fileNames + Util.Helper.CreateString(" ", MAX_SAVE_FILE_LENGTH - fileNames.Length));
            sfn.file = System.Runtime.InteropServices.Marshal.StringToBSTR(fileName + Crosstales.FB.Util.Helper.CreateString(" ", MAX_FILE_LENGTH - fileName.Length));
            sfn.maxFile = MAX_FILE_LENGTH;
            sfn.title = title;
            sfn.defExt = defaultExtension;
            sfn.flags = (int)FOS.FOS_NOCHANGEDIR | (int)FOS.FOS_PATHMUSTEXIST | (FileBrowser.Instance.AskOverwriteFile ? (int)FOS.FOS_OVERWRITEPROMPT : 0);
            sfn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(sfn);

            //isFirsttime = false;

            if (NativeMethods.GetSaveFileName(ref sfn))
            {
               string file = System.Runtime.InteropServices.Marshal.PtrToStringUni(sfn.file);
               //string file = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(sfn.file);

               if (string.IsNullOrEmpty(file))
               {
                  instance.CurrentSaveFile = string.Empty;

                  return null;
               }

               string newFile = Crosstales.Common.Util.FileHelper.ValidateFile(file);
               //return newFile.Substring(0, newFile.Length - 1);

               string path = newFile.Substring(0, newFile.Length);

               instance.CurrentSaveFile = path;

               return instance.CurrentSaveFile;
            }

            instance.CurrentSaveFile = string.Empty;
         }
         catch (Exception ex)
         {
            instance.CurrentSaveFile = string.Empty;

            Debug.LogError($"Save file dialog threw an error: {ex}");
         }

         return null;
      }

      [AOT.MonoPInvokeCallback(typeof(NativeMethods.BrowseCallbackProc))]
      private static int onBrowseEvent(IntPtr hWnd, int msg, IntPtr lp, IntPtr lpData)
      {
         switch (msg)
         {
            case BFFM_INITIALIZED:
               NativeMethods.SendMessage(new System.Runtime.InteropServices.HandleRef(null, hWnd), BFFM_SETSELECTIONW, 1, _initialPath);
               break;
            case BFFM_SELCHANGED:
            {
               IntPtr pathPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(260 * System.Runtime.InteropServices.Marshal.SystemDefaultCharSize);

               if (NativeMethods.SHGetPathFromIDList(lp, pathPtr))
                  NativeMethods.SendMessage(new System.Runtime.InteropServices.HandleRef(null, hWnd), BFFM_SETSTATUSTEXTW, 0, pathPtr);

               System.Runtime.InteropServices.Marshal.FreeHGlobal(pathPtr);
               break;
            }
         }

         return 0;
      }

/*
private string getDirectory(string directory, bool addEndDelimiter = true)
{
    string result = string.Empty;

    if (!string.IsNullOrEmpty(directory))
    {
        result = directory.Replace('/', '\\');

        if (addEndDelimiter)
        {
            if (!result.EndsWith(Util.Constants.PATH_DELIMITER_WINDOWS))
            {
                result += Util.Constants.PATH_DELIMITER_WINDOWS;
            }
        }

        return result;
    }

    return directory;
}
*/

      private static string getDefaultExtension(ExtensionFilter[] extensions)
      {
         if (extensions?.Length > 0 && extensions[0].Extensions.Length > 0)
            return extensions[0].Extensions[0];

         return "*";
      }

      private static string getFilterFromFileExtensionList(ExtensionFilter[] extensions)
      {
         if (extensions?.Length > 0)
         {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (ExtensionFilter filter in extensions)
            {
               sb.Append(filter.Name);
               sb.Append("\0");

               for (int ii = 0; ii < filter.Extensions.Length; ii++)
               {
                  sb.Append("*.");
                  sb.Append(filter.Extensions[ii]);

                  if (ii + 1 < filter.Extensions.Length)
                     sb.Append(";");
               }

               sb.Append("\0");
            }

            sb.Append("\0");

            if (Crosstales.FB.Util.Config.DEBUG)
               Debug.Log($"getFilterFromFileExtensionList: {sb}");

            return sb.ToString();
         }

         return $"{instance.allFilesText}\0*.*\0\0";
      }

      #endregion
   }

   internal static class NativeMethods
   {
      public delegate int BrowseCallbackProc(IntPtr hwnd, int uMsg, IntPtr lParam, IntPtr lpData);

      [System.Runtime.InteropServices.DllImport("Comdlg32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      public static extern bool GetOpenFileName(ref OpenFileName ofn);

      [System.Runtime.InteropServices.DllImport("Comdlg32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      public static extern bool GetSaveFileName(ref OpenFileName sfn);

      [System.Runtime.InteropServices.DllImport("shell32.dll")]
      internal static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

      [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      internal static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

      [System.Runtime.InteropServices.DllImport("user32.dll")]
      internal static extern IntPtr GetActiveWindow();

      [System.Runtime.InteropServices.DllImport("user32.dll", PreserveSig = true)]
      public static extern IntPtr SendMessage(System.Runtime.InteropServices.HandleRef hWnd, uint Msg, int wParam, IntPtr lParam);

      [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      public static extern IntPtr SendMessage(System.Runtime.InteropServices.HandleRef hWnd, int msg, int wParam, string lParam);

/*
      [System.Runtime.InteropServices.DllImportAttribute("shell32.dll")]
      private static extern int SHILCreateFromPath([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
         string pszPath, out System.IntPtr ppIdl, ref uint rgflnOut);

      [System.Runtime.InteropServices.DllImportAttribute("shell32.dll")]
      private static extern int SHCreateShellItem(System.IntPtr pidlParent, System.IntPtr psfParent, System.IntPtr pidl, out IShellItem ppsi);
*/
      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      internal struct OpenFileName
      {
         public int structSize;
         public IntPtr dlgOwner;
         public IntPtr instance;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string filter;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] public string customFilter;
         public int maxCustFilter;
         public int filterIndex;
         public IntPtr file;
         public int maxFile;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] public string fileTitle;
         public int maxFileTitle;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string initialDir;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string title;
         public int flags;
         public ushort fileOffset;
         public ushort fileExtension;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string defExt;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string custData;
         public IntPtr hook;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string templateName;
         public IntPtr reservedPtr;
         public int reservedInt;
         public int flagsEx;
      }

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      internal struct BROWSEINFO
      {
         public IntPtr dlgOwner;
         public IntPtr pidlRoot;
         public IntPtr pszDisplayName;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] public string lpszTitle;
         public uint ulFlags;
         public BrowseCallbackProc lpfn;
         public IntPtr lParam;
         public int iImage;
      }

      [System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("43826D1E-E718-42EE-BC55-A1E261C37BFE"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
      public interface IShellItem
      {
         void BindToHandler(); // not fully defined
         void GetParent(); // not fully defined
         void GetDisplayName([System.Runtime.InteropServices.InAttribute] SIGDN sigdnName, [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string ppszName);
         void GetAttributes(); // not fully defined
         void Compare(); // not fully defined
      }

      [System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
      internal class FileOpenDialog
      {
      }

      //[System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("42f85136-db7e-439c-85f1-e4075d135fc8"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
      [System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("d57c7288-d4ad-4768-be02-9d969532d960"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
      internal interface IFileOpenDialog
      {
         [System.Runtime.InteropServices.PreserveSig]
         uint Show([System.Runtime.InteropServices.In] System.IntPtr parent); // IModalWindow

         void SetFileTypes(); // not fully defined
         void SetFileTypeIndex([System.Runtime.InteropServices.InAttribute] uint iFileType);
         void GetFileTypeIndex(out uint piFileType);
         void Advise(); // not fully defined
         void Unadvise();
         void SetOptions([System.Runtime.InteropServices.InAttribute] FOS fos);
         void GetOptions(out FOS pfos);
         void SetDefaultFolder(IShellItem psi);
         void SetFolder([System.Runtime.InteropServices.InAttribute] IShellItem psi);
         void GetFolder(out IShellItem ppsi);
         void GetCurrentSelection(out IShellItem ppsi);
         void SetFileName([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
         void GetFileName([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string pszName);
         void SetTitle([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszTitle);
         void SetOkButtonLabel([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszText);
         void SetFileNameLabel([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszLabel);
         void GetResult(out IShellItem ppsi);
         void AddPlace(IShellItem psi, int alignment);
         void SetDefaultExtension([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDefaultExtension);
         void Close(int hr);
         void SetClientGuid(); // not fully defined
         void ClearClientData();
         void SetFilter([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Interface)] System.IntPtr pFilter);
         void GetResults([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Interface)] out System.IntPtr ppenum); // not fully defined
         void GetSelectedItems([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Interface)] out System.IntPtr ppsai); // not fully defined
      }

/*
      [System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("b4db1657-70d7-485e-8e3e-6fcb5a5c1802"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
      internal interface IModalWindow
      {
         //[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall, MethodCodeType = System.Runtime.CompilerServices.MethodCodeType.Runtime), System.Runtime.InteropServices.PreserveSigAttribute]
         [System.Runtime.InteropServices.PreserveSig]
         uint Show([System.Runtime.InteropServices.InAttribute] IntPtr parent);
      }

      [System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("42f85136-db7e-439c-85f1-e4075d135fc8"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
      internal interface IFileDialog : IModalWindow
      {
         // Defined in IModalWindow - repeated here due to requirements of COM interop layer
         // --------------------------------------------------------------------------------
         //[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall, MethodCodeType = System.Runtime.CompilerServices.MethodCodeType.Runtime), System.Runtime.InteropServices.PreserveSigAttribute]
         //[System.Runtime.InteropServices.PreserveSig]
          //uint Show([System.Runtime.InteropServices.InAttribute] IntPtr parent);

         //[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall, MethodCodeType = System.Runtime.CompilerServices.MethodCodeType.Runtime)]
         [System.Runtime.InteropServices.PreserveSig]
         void SetOptions([System.Runtime.InteropServices.InAttribute] FOS fos);
      }

      [System.Runtime.InteropServices.ComImportAttribute, System.Runtime.InteropServices.GuidAttribute("d57c7288-d4ad-4768-be02-9d969532d960"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
      internal interface IFileOpenDialog : IFileDialog
      {
         // Defined in IModalWindow - repeated here due to requirements of COM interop layer
         // --------------------------------------------------------------------------------
         //[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall, MethodCodeType = System.Runtime.CompilerServices.MethodCodeType.Runtime), System.Runtime.InteropServices.PreserveSigAttribute]
         //[System.Runtime.InteropServices.PreserveSig]
         //uint Show([System.Runtime.InteropServices.In] System.IntPtr parent); // IModalWindow

         //[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall, MethodCodeType = System.Runtime.CompilerServices.MethodCodeType.Runtime)]
         //[System.Runtime.InteropServices.PreserveSig]
         //void SetOptions([System.Runtime.InteropServices.InAttribute] FOS fos);

         void SetFileTypes(); // not fully defined
         void SetFileTypeIndex([System.Runtime.InteropServices.InAttribute] uint iFileType);
         void GetFileTypeIndex(out uint piFileType);
         void Advise(); // not fully defined
         void Unadvise();
         void GetOptions(out FOS pfos);
         void SetDefaultFolder(IShellItem psi);
         void SetFolder([System.Runtime.InteropServices.InAttribute] IShellItem psi);
         void GetFolder(out IShellItem ppsi);
         void GetCurrentSelection(out IShellItem ppsi);
         void SetFileName([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
         void GetFileName([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string pszName);
         void SetTitle([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszTitle);
         void SetOkButtonLabel([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszText);
         void SetFileNameLabel([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszLabel);
         void GetResult(out IShellItem ppsi);
         void AddPlace(IShellItem psi, int alignment);
         void SetDefaultExtension([System.Runtime.InteropServices.InAttribute, System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDefaultExtension);
         void Close(int hr);
         void SetClientGuid(); // not fully defined
         void ClearClientData();
         void SetFilter([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Interface)] System.IntPtr pFilter);
         void GetResults([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Interface)] out System.IntPtr ppenum); // not fully defined
         void GetSelectedItems([System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Interface)] out System.IntPtr ppsai); // not fully defined
      }
      */
   }

   [Flags]
   internal enum FOS
   {
      FOS_ALLNONSTORAGEITEMS = 0x80,
      FOS_ALLOWMULTISELECT = 0x200,
      FOS_CREATEPROMPT = 0x2000,
      FOS_DEFAULTNOMINIMODE = 0x20000000,
      FOS_DONTADDTORECENT = 0x2000000,
      FOS_FILEMUSTEXIST = 0x1000,
      FOS_FORCEFILESYSTEM = 0x40,
      FOS_FORCESHOWHIDDEN = 0x10000000,
      FOS_HIDEMRUPLACES = 0x20000,
      FOS_HIDEPINNEDPLACES = 0x40000,
      FOS_NOCHANGEDIR = 8,
      FOS_NODEREFERENCELINKS = 0x100000,
      FOS_NOREADONLYRETURN = 0x8000,
      FOS_NOTESTFILECREATE = 0x10000,
      FOS_NOVALIDATE = 0x100,
      FOS_OVERWRITEPROMPT = 2,
      FOS_PATHMUSTEXIST = 0x800,
      FOS_PICKFOLDERS = 0x20,
      FOS_SHAREAWARE = 0x4000,
      FOS_STRICTFILETYPES = 4
   }

   internal enum SIGDN : uint
   {
      SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
      SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
      SIGDN_FILESYSPATH = 0x80058000,
      SIGDN_NORMALDISPLAY = 0,
      SIGDN_PARENTRELATIVE = 0x80080001,
      SIGDN_PARENTRELATIVEEDITING = 0x80031001,
      SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
      SIGDN_PARENTRELATIVEPARSING = 0x80018001,
      SIGDN_URL = 0x80068000
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)