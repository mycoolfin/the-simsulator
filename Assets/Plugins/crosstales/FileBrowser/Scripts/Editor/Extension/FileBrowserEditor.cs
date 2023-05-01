#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.FB.EditorUtil;
using Crosstales.FB.Wrapper;

namespace Crosstales.FB.EditorExtension
{
   /// <summary>Custom editor for the 'FileBrowser'-class.</summary>
   [InitializeOnLoad]
   [CustomEditor(typeof(FileBrowser))]
   public class FileBrowserEditor : Editor
   {
      #region Variables

      private FileBrowser script;
      private string path;

      private Object customWrapper;
      private bool customMode;
      private bool legacyFolderBrowser;
      private bool askOverwriteFile;
      private bool alwaysReadFile;
      private bool dontDestroy;

      private string titleOpenFile;
      private string titleOpenFiles;
      private string titleOpenFolder;
      private string titleOpenFolders;
      private string titleSaveFile;

      private string textAllFiles;
      private string nameSaveFile;

      #endregion


      #region Static constructor

      static FileBrowserEditor()
      {
         EditorApplication.hierarchyWindowItemOnGUI += hierarchyItemCB;
      }

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (FileBrowser)target;

         EditorApplication.update += onUpdate;
         //TRManager.OnQuotaUpdate += onUpdateQuota;

         //onUpdate();
      }

      private void OnDisable()
      {
         EditorApplication.update -= onUpdate;
         //TRManager.OnQuotaUpdate -= onUpdateQuota;
      }

      public override void OnInspectorGUI()
      {
         if (!script.CustomMode && !script.isPlatformSupported)
            EditorGUILayout.HelpBox("The current platform is not supported by the File Browser. Please add a custom wrapper (e.g. Runtime File Browser).", MessageType.Error);

         serializedObject.Update();

         GUILayout.Label("Custom Wrapper", EditorStyles.boldLabel);

         customMode = EditorGUILayout.BeginToggleGroup(new GUIContent("Active", "Enables or disables the custom wrapper (default: false)."), script.CustomMode);
         if (customMode != script.CustomMode)
         {
            script.CustomMode = customMode;

            serializedObject.FindProperty("customMode").boolValue = customMode;
            serializedObject.ApplyModifiedProperties();
         }

         EditorGUI.indentLevel++;

         customWrapper = EditorGUILayout.ObjectField("Custom Wrapper", script.CustomWrapper, typeof(BaseCustomFileBrowser), true);
         if (customWrapper != script.CustomWrapper)
         {
            script.CustomWrapper = (BaseCustomFileBrowser)customWrapper;

            serializedObject.FindProperty("customWrapper").objectReferenceValue = customWrapper;
            serializedObject.ApplyModifiedProperties();
         }

         EditorGUI.indentLevel--;
         EditorGUILayout.EndToggleGroup();

         if (customMode)
         {
            if (script.CustomWrapper == null)
            {
               EditorGUILayout.HelpBox("'Custom Wrapper' is null! Please add a valid wrapper.", MessageType.Warning);
            }
            else
            {
               if (!script.CustomWrapper.isPlatformSupported)
               {
                  EditorGUILayout.HelpBox("'Custom Wrapper' does not support the current platform!", MessageType.Warning);
               }
            }
         }

         GUILayout.Space(8);
         GUILayout.Label("Titles", EditorStyles.boldLabel);

         titleOpenFile = EditorGUILayout.TextField(new GUIContent("Open File", "Title for the 'Open File'-dialog."), script.TitleOpenFile);
         if (!titleOpenFile.Equals(script.TitleOpenFile))
         {
            serializedObject.FindProperty("titleOpenFile").stringValue = titleOpenFile;
            serializedObject.ApplyModifiedProperties();
         }

         titleOpenFiles = EditorGUILayout.TextField(new GUIContent("Open Files", "Title for the 'Open Files'-dialog."), script.TitleOpenFiles);
         if (!titleOpenFiles.Equals(script.TitleOpenFiles))
         {
            serializedObject.FindProperty("titleOpenFiles").stringValue = titleOpenFiles;
            serializedObject.ApplyModifiedProperties();
         }

         titleOpenFolder = EditorGUILayout.TextField(new GUIContent("Open Folder", "Title for the 'Open Folder'-dialog."), script.TitleOpenFolder);
         if (!titleOpenFolder.Equals(script.TitleOpenFolder))
         {
            serializedObject.FindProperty("titleOpenFolder").stringValue = titleOpenFolder;
            serializedObject.ApplyModifiedProperties();
         }

         titleOpenFolders = EditorGUILayout.TextField(new GUIContent("Open Folders", "Title for the 'Open Folders'-dialog."), script.TitleOpenFolders);
         if (!titleOpenFolders.Equals(script.TitleOpenFolders))
         {
            serializedObject.FindProperty("titleOpenFolders").stringValue = titleOpenFolders;
            serializedObject.ApplyModifiedProperties();
         }

         titleSaveFile = EditorGUILayout.TextField(new GUIContent("Save File", "Title for the 'Save File'-dialog."), script.TitleSaveFile);
         if (!titleSaveFile.Equals(script.TitleSaveFile))
         {
            serializedObject.FindProperty("titleSaveFile").stringValue = titleSaveFile;
            serializedObject.ApplyModifiedProperties();
         }

         GUILayout.Space(8);
         GUILayout.Label("Labels", EditorStyles.boldLabel);

         textAllFiles = EditorGUILayout.TextField(new GUIContent("All Files (*)", "Text for 'All Files'-filter (*)."), script.TextAllFiles);
         if (!textAllFiles.Equals(script.TextAllFiles))
         {
            serializedObject.FindProperty("textAllFiles").stringValue = textAllFiles;
            serializedObject.ApplyModifiedProperties();
         }

         nameSaveFile = EditorGUILayout.TextField(new GUIContent("Name Save File", "Default name of the save-file."), script.NameSaveFile);
         if (!nameSaveFile.Equals(script.NameSaveFile))
         {
            serializedObject.FindProperty("nameSaveFile").stringValue = nameSaveFile;
            serializedObject.ApplyModifiedProperties();
         }

         GUILayout.Space(8);
         GUILayout.Label("Windows Settings", EditorStyles.boldLabel);

         legacyFolderBrowser = EditorGUILayout.Toggle(new GUIContent("Legacy Folder Browser", "Use the legacy folder browser under Windows (default: false)."), script.LegacyFolderBrowser);
         if (legacyFolderBrowser != script.LegacyFolderBrowser)
         {
            serializedObject.FindProperty("legacyFolderBrowser").boolValue = legacyFolderBrowser;
            serializedObject.ApplyModifiedProperties();
         }

         askOverwriteFile = EditorGUILayout.Toggle(new GUIContent("Ask Overwrite File", "Ask to overwrite existing file in save dialog (default: true)."), script.AskOverwriteFile);
         if (askOverwriteFile != script.AskOverwriteFile)
         {
            serializedObject.FindProperty("askOverwriteFile").boolValue = askOverwriteFile;
            serializedObject.ApplyModifiedProperties();
         }

         GUILayout.Space(8);
         GUILayout.Label("UWP (WSA) Settings", EditorStyles.boldLabel);

         alwaysReadFile = EditorGUILayout.Toggle(new GUIContent("Always Read File", "Always read the file data under UWP (default: false)."), script.AlwaysReadFile);
         if (alwaysReadFile != script.AlwaysReadFile)
         {
            serializedObject.FindProperty("alwaysReadFile").boolValue = alwaysReadFile;
            serializedObject.ApplyModifiedProperties();
         }

         GUILayout.Space(8);
         GUILayout.Label("Behaviour Settings", EditorStyles.boldLabel);

         dontDestroy = EditorGUILayout.Toggle(new GUIContent("Dont Destroy", "Don't destroy gameobject during scene switches (default: true)."), script.DontDestroy);
         if (dontDestroy != script.DontDestroy)
         {
            serializedObject.FindProperty("dontDestroy").boolValue = dontDestroy;
            serializedObject.ApplyModifiedProperties();
         }

         EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            if (!script.isPlatformSupported)
            {
               EditorGUILayout.HelpBox("The current platform is not supported in builds!", MessageType.Error);
            }
            else
            {
               GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

               if (Util.Helper.isEditorMode)
               {
                  if (script.isWorkingInEditor)
                  {
                     GUILayout.Space(6);

                     if (GUILayout.Button(new GUIContent(" Open Single File", EditorHelper.Icon_File, "Opens a single file.")))
                        path = FileBrowser.Instance.OpenSingleFile();

                     GUILayout.Space(6);

                     if (GUILayout.Button(new GUIContent(" Open Single Folder", EditorHelper.Icon_Folder, "Opens a single folder.")))
                        path = FileBrowser.Instance.OpenSingleFolder();

                     GUILayout.Space(6);

                     if (GUILayout.Button(new GUIContent(" Save File", EditorHelper.Icon_Save, "Saves a file.")))
                        path = FileBrowser.Instance.SaveFile();

                     GUILayout.Space(6);

                     //GUILayout.Label($"Path: {(string.IsNullOrEmpty(path) ? "nothing selected" : path)}");
                     EditorGUILayout.SelectableLabel($"{(string.IsNullOrEmpty(path) ? "Path: nothing selected" : path)}");

                     GUILayout.Space(6);
                  }
                  else
                  {
                     EditorGUILayout.HelpBox("Test-Drive is not supported for the current wrapper/platform.", MessageType.Info);
                  }
               }
               else
               {
                  EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
               }
            }
         }
         else
         {
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      #endregion


      #region Private methods

      private void onUpdate()
      {
         Repaint();
      }

/*
      private void onUpdateQuota(int e)
      {
         //Debug.Log("Quota: " + e, this);
         Repaint();
      }
*/
      private static void hierarchyItemCB(int instanceID, Rect selectionRect)
      {
         if (EditorConfig.HIERARCHY_ICON)
         {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (go != null && go.GetComponent<FileBrowser>())
            {
               Rect r = new Rect(selectionRect);
               r.x = r.width - 4;

               GUI.Label(r, EditorHelper.Logo_Asset_Small);
            }
         }
      }

      #endregion
   }
}
#endif
// © 2020-2023 crosstales LLC (https://www.crosstales.com)