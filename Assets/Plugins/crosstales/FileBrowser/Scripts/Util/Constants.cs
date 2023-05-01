namespace Crosstales.FB.Util
{
   /// <summary>Collected constants of very general utility for the asset.</summary>
   public abstract class Constants : Crosstales.Common.Util.BaseConstants
   {
      #region Constant variables

      /// <summary>Name of the asset.</summary>
      public const string ASSET_NAME = "File Browser PRO";

      /// <summary>Short name of the asset.</summary>
      public const string ASSET_NAME_SHORT = "FB PRO";

      /// <summary>Version of the asset.</summary>
      public const string ASSET_VERSION = "2023.1.0";

      /// <summary>Build number of the asset.</summary>
      public const int ASSET_BUILD = 20230126;

      /// <summary>Create date of the asset (YYYY, MM, DD).</summary>
      public static readonly System.DateTime ASSET_CREATED = new System.DateTime(2017, 8, 1);

      /// <summary>Change date of the asset (YYYY, MM, DD).</summary>
      public static readonly System.DateTime ASSET_CHANGED = new System.DateTime(2023, 1, 26);

      /// <summary>URL of the PRO asset in UAS.</summary>
      public const string ASSET_PRO_URL = "https://assetstore.unity.com/packages/slug/98713?aid=1011lNGT";

      /// <summary>URL for update-checks of the asset</summary>
      public const string ASSET_UPDATE_CHECK_URL = "https://www.crosstales.com/media/assets/fb_versions.txt";
      //public const string ASSET_UPDATE_CHECK_URL = "https://www.crosstales.com/media/assets/test/fb_versions_test.txt";

      /// <summary>Contact to the owner of the asset.</summary>
      public const string ASSET_CONTACT = "fb@crosstales.com";

      /// <summary>URL of the asset manual.</summary>
      public const string ASSET_MANUAL_URL = "https://www.crosstales.com/media/data/assets/FileBrowser/FileBrowser-doc.pdf";

      /// <summary>URL of the asset API.</summary>
      public const string ASSET_API_URL = "https://www.crosstales.com/media/data/assets/FileBrowser/api/";

      /// <summary>URL of the asset forum.</summary>
      public const string ASSET_FORUM_URL = "https://forum.unity.com/threads/file-browser-native-file-browser-for-standalone.510403/";

      /// <summary>URL of the asset in crosstales.</summary>
      public const string ASSET_WEB_URL = "https://www.crosstales.com/en/portfolio/FileBrowser/";

/*
      /// <summary>URL of the promotion video of the asset (Youtube).</summary>
      public const string ASSET_VIDEO_PROMO = "TBD"; //TODO set correct URL
*/

      /// <summary>URL of the tutorial video of the asset (Youtube).</summary>
      public const string ASSET_VIDEO_TUTORIAL = "https://youtu.be/nczXecD0uB0?list=PLgtonIOr6Tb41XTMeeZ836tjHlKgOO84S";

      /// <summary>URL of the 3rd party asset "Runtime File Browser".</summary>
      public const string ASSET_3P_RTFB = "https://assetstore.unity.com/packages/slug/113006?aid=1011lNGT";

      /// <summary>URL of the 3rd party asset "WebGL Native File Browser".</summary>
      public const string ASSET_3P_WEBGL = "https://assetstore.unity.com/packages/slug/41902?aid=1011lNGT";

      // Keys for the configuration of the asset
      public const string KEY_PREFIX = "FILEBROWSER_CFG_";
      public const string KEY_ASSET_PATH = KEY_PREFIX + "ASSET_PATH";
      public const string KEY_DEBUG = KEY_PREFIX + "DEBUG";
      public const string KEY_NATIVE_WINDOWS = KEY_PREFIX + "NATIVE_WINDOWS";

      // Default values
      public const bool DEFAULT_NATIVE_WINDOWS = false;

      /// <summary>FB prefab scene name.</summary>
      public const string FB_SCENE_OBJECT_NAME = "FileBrowser";

      #endregion


      #region Changable variables

      /// <summary>Minimal number of selectable files under Windows with a path length of 260 (default: 256).</summary>
      public static int WINDOWS_MIN_OPEN_NUMBER_OF_FILES = 256;

      #endregion
   }
}
// © 2017-2023 crosstales LLC (https://www.crosstales.com)