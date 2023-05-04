﻿#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using Crosstales.FB.EditorUtil;
using Crosstales.FB.Util;

namespace Crosstales.FB.EditorTask
{
   /// <summary>Checks for updates of the asset.</summary>
   [InitializeOnLoad]
   public static class UpdateCheck
   {
      #region Variables

      public const string TEXT_NOT_CHECKED = "Not checked.";
      public const string TEXT_NO_UPDATE = "No update available - you are using the latest version.";

      private static UpdateStatus status = UpdateStatus.NOT_CHECKED;

      private static readonly char[] splitChar = { ';' };

      #endregion


      #region Constructor

      static UpdateCheck()
      {
         if (EditorConfig.UPDATE_CHECK)
         {
            if (Config.DEBUG)
               Debug.Log("Updater enabled!");

            string lastDate = EditorPrefs.GetString(EditorConstants.KEY_UPDATE_DATE);
            string date = System.DateTime.Now.ToString("yyyyMMdd"); // every day
            //string date = System.DateTime.Now.ToString("yyyyMMddHHmm"); // every minute (for tests)

            if (Constants.DEV_DEBUG)
               Debug.Log($"Last check: {lastDate}");

            if (!date.Equals(lastDate))
            {
               if (Crosstales.Common.Util.NetworkHelper.isInternetAvailable)
               {
                  if (Config.DEBUG)
                     Debug.Log("Checking for update...");

                  //new System.Threading.Thread(() => updateCheck()).Start();
                  updateCheck();

                  EditorPrefs.SetString(EditorConstants.KEY_UPDATE_DATE, date);
               }
               else
               {
                  if (Config.DEBUG)
                     Debug.Log("No Internet available!");
               }
            }
            else
            {
               if (Config.DEBUG)
                  Debug.Log("No update check needed.");
            }
         }
         else
         {
            if (Config.DEBUG)
               Debug.Log("Updater disabled!");
         }
      }

      #endregion


      #region Static methods

      public static void UpdateCheckForEditor(out string result, out UpdateStatus st)
      {
         string[] data = readData();

         updateStatus(data);

         switch (status)
         {
            case UpdateStatus.UPDATE:
               result = updateTextForEditor(data);
               break;
            case UpdateStatus.UPDATE_VERSION:
               result = updateVersionTextForEditor(data);
               break;
            case UpdateStatus.DEPRECATED:
               result = deprecatedTextForEditor(data);
               break;
            default:
               result = TEXT_NO_UPDATE;
               break;
         }

         st = status;
      }

      public static void UpdateCheckWithDialog()
      {
         string[] data = readData();

         updateStatus(data);

         switch (status)
         {
            case UpdateStatus.UPDATE:
            {
               bool option = EditorUtility.DisplayDialog($"{Constants.ASSET_NAME} - Update available",
                  updateText(data),
                  "Yes, let's do it!",
                  "Not right now");

               if (option)
                  Crosstales.Common.Util.NetworkHelper.OpenURL(EditorConstants.ASSET_URL);
               break;
            }
            case UpdateStatus.UPDATE_VERSION:
            {
               bool option = EditorUtility.DisplayDialog($"{Constants.ASSET_NAME} - Upgrade needed",
                  updateVersionText(data),
                  "Yes, let's do it!",
                  "Not right now");

               if (option)
                  Crosstales.Common.Util.NetworkHelper.OpenURL(EditorConstants.ASSET_URL);
               break;
            }
            case UpdateStatus.DEPRECATED:
            {
               bool option = EditorUtility.DisplayDialog($"{Constants.ASSET_NAME} - Upgrade needed",
                  deprecatedText(data),
                  "Learn more",
                  "Not right now");

               if (option)
                  Crosstales.Common.Util.NetworkHelper.OpenURL(EditorConstants.ASSET_URL);
               break;
            }
            default:
            {
               EditorUtility.DisplayDialog($"{Constants.ASSET_NAME} - Latest version {Constants.ASSET_VERSION}",
                  TEXT_NO_UPDATE,
                  "OK");
               break;
            }
         }
      }

      #endregion


      #region Private methods

      private static void updateCheck()
      {
         string[] data = readData();

         updateStatus(data);

         switch (status)
         {
            case UpdateStatus.UPDATE:
            {
               int option = EditorUtility.DisplayDialogComplex($"{Constants.ASSET_NAME} - Update available",
                  updateText(data),
                  "Yes, let's do it!",
                  "Not right now",
                  "Don't check again!");

               switch (option)
               {
                  case 0:
                     Crosstales.Common.Util.NetworkHelper.OpenURL(EditorConstants.ASSET_URL);
                     break;
                  case 1:
                     // do nothing!
                     break;
                  default:
                     EditorConfig.UPDATE_CHECK = false;

                     EditorConfig.Save();
                     break;
               }

               break;
            }
            case UpdateStatus.UPDATE_VERSION:
            {
               int option = EditorUtility.DisplayDialogComplex($"{Constants.ASSET_NAME} - Upgrade needed",
                  updateVersionText(data),
                  "Yes, let's do it!",
                  "Not right now",
                  "Don't ask again!");

               switch (option)
               {
                  case 0:
                     Crosstales.Common.Util.NetworkHelper.OpenURL(EditorConstants.ASSET_URL);
                     break;
                  case 1:
                     // do nothing!
                     break;
                  default:
                     EditorConfig.UPDATE_CHECK = false;

                     EditorConfig.Save();
                     break;
               }

               break;
            }
            case UpdateStatus.DEPRECATED:
            {
               int option = EditorUtility.DisplayDialogComplex($"{Constants.ASSET_NAME} - Upgrade needed",
                  deprecatedText(data),
                  "Learn more",
                  "Not right now",
                  "Don't bother me again!");

               switch (option)
               {
                  case 0:
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_AUTHOR_URL);
                     break;
                  case 1:
                     // do nothing!
                     break;
                  default:
                     EditorConfig.UPDATE_CHECK = false;

                     EditorConfig.Save();
                     break;
               }

               break;
            }
            default:
            {
               if (Config.DEBUG)
                  Debug.Log("Asset is up-to-date.");
               break;
            }
         }
      }

      private static string updateText(string[] data)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (data != null)
         {
            sb.Append("Your version:\t");
            sb.Append(Constants.ASSET_VERSION);
            sb.Append(System.Environment.NewLine);
            sb.Append("New version:\t");
            sb.Append(data[2]);
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);
            sb.AppendLine("Please download the new version from the Unity AssetStore!");
         }

         return sb.ToString();
      }

      private static string updateVersionText(string[] data)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (data != null)
         {
            sb.Append(Constants.ASSET_NAME);
            sb.Append(" is deprecated in favour of an newer version!");
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);
            sb.AppendLine("Please consider an upgrade in the Unity AssetStore.");
         }

         return sb.ToString();
      }

      private static string deprecatedText(string[] data)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (data != null)
         {
            sb.Append(Constants.ASSET_NAME);
            sb.Append(" is deprecated!");
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);
            sb.AppendLine("Please check the link for more information:");
            sb.AppendLine(Constants.ASSET_AUTHOR_URL);
         }

         return sb.ToString();
      }

      private static string[] readData()
      {
         string[] data = null;

         try
         {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = Crosstales.Common.Util.NetworkHelper.RemoteCertificateValidationCallback;

            using (System.Net.WebClient client = new Crosstales.Common.Util.CTWebClient())
            {
               string content = client.DownloadString(Constants.ASSET_UPDATE_CHECK_URL);

               foreach (string line in Helper.SplitStringToLines(content).Where(line => line.CTStartsWith(EditorConstants.ASSET_UID.ToString())))
               {
                  data = line.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                  if (data.Length >= 3)
                  {
                     //valid record?
                     break;
                  }

                  data = null;
               }
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not load update file: {ex}");
         }

         return data;
      }

      private static void updateStatus(string[] data)
      {
         if (data != null)
         {
            if (int.TryParse(data[1], out int buildNumber))
            {
               if (buildNumber > Constants.ASSET_BUILD)
               {
                  status = UpdateStatus.UPDATE;
               }
               else
                  switch (buildNumber)
                  {
                     case -200:
                        status = UpdateStatus.UPDATE_VERSION;
                        break;
                     case -900:
                        status = UpdateStatus.DEPRECATED;
                        break;
                     default:
                        status = UpdateStatus.NO_UPDATE;
                        break;
                  }
            }
         }
      }

      private static string updateTextForEditor(string[] data)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (data != null)
         {
            sb.AppendLine("Update found!");
            sb.Append(System.Environment.NewLine);
            sb.Append("Your version:\t");
            sb.Append(Constants.ASSET_VERSION);
            sb.Append(System.Environment.NewLine);
            sb.Append("New version:\t");
            sb.Append(data[2]);
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);
            sb.AppendLine("Please download the new version from the Unity AssetStore.");
         }

         return sb.ToString();
      }

      private static string updateVersionTextForEditor(string[] data)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (data != null)
         {
            sb.Append(Constants.ASSET_NAME);
            sb.Append(" is deprecated in favour of an newer version!");
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);
            sb.AppendLine("Please consider an upgrade in the Unity AssetStore.");
         }

         return sb.ToString();
      }

      private static string deprecatedTextForEditor(string[] data)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (data != null)
         {
            sb.Append(Constants.ASSET_NAME);
            sb.Append(" is deprecated!");
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);
            sb.AppendLine("Please click below for more information.");
         }

         return sb.ToString();
      }

      #endregion
   }

   /// <summary>All possible update stati.</summary>
   public enum UpdateStatus
   {
      NOT_CHECKED,
      NO_UPDATE,
      UPDATE,
      UPDATE_VERSION,
      DEPRECATED
   }
}
#endif
// © 2017-2023 crosstales LLC (https://www.crosstales.com)