using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif
using UnityEditorInternal;
using UnityEngine;

namespace LocalizedAppTitleNamespace
{
	[System.Serializable]
	public class LocalizedData
	{
		public string LanguageCode = "en";
		public string AppName = "My Game";
		public string AppShortName = "";
		public Texture2D AndroidIcon, iOSIcon;
	}

	[System.Serializable]
	public class Settings
	{
		private const string SAVE_PATH = "ProjectSettings/LocalizedAppTitle.json";
		private const string ANDROID_RESOURCES_LIBRARY_PATH = "Assets/Plugins/Android/LocalizedAppTitle_AUTOGENERATED.aar";

		private const float SPACE_BETWEEN_LIST_ELEMENTS = 6f;
		private const float SPACE_BETWEEN_INPUT_FIELDS = 2f;

		public bool ReplaceApplicationProductName;
		public bool LocalizeAppNameOnAndroid = true;
		public bool LocalizeAppIconOnAndroid = true;
		public bool LocalizeAppNameOniOS = true;
		public bool LocalizeAppIconOniOS = true;

		public List<LocalizedData> LocalizedData = new List<LocalizedData>();
		public int DefaultLocalizedData;
		public string AndroidNameResource = "app_name", AndroidIconResource = "app_icon";

		private ReorderableList localizedDataDrawer;

		private readonly GUIContent localizeAppIconOniOSLabel = new GUIContent( "Localize App Icon on iOS*", "Icon WON'T change automatically with the system language but it's possible to change it manually via 'LocalizedAppTitle.SetLanguage' function" );
		private readonly GUIContent languageCodeLabel = new GUIContent( "Language Code", "Note that language sub-codes aren't supported on Android (e.g. 'en-US', use 'en' instead)" );
		private readonly GUIContent appShortNameLabel = new GUIContent( "App Short Name", "(Optional) (iOS only) Maximum 15 characters" );
		private readonly GUIContent androidNameResourceLabel = new GUIContent( "\"android:label\" Resource", "Can be learnt by looking at the AndroidManifest.xml file; Unity's default value is 'app_name'" );
		private readonly GUIContent androidIconResourceLabel = new GUIContent( "\"android:icon\" Resource", "Can be learnt by looking at the AndroidManifest.xml file; Unity's default value is 'app_icon'" );
		private readonly GUIContent generateAndroidResourcesLabel = new GUIContent( "Generate Android Resources", "You MUST generate Android resources after modifying the localization parameters!" );

		private static Settings m_instance = null;
		public static Settings Instance
		{
			get
			{
				if( m_instance == null )
				{
					try
					{
						m_instance = new Settings();
						if( File.Exists( SAVE_PATH ) )
							EditorJsonUtility.FromJsonOverwrite( File.ReadAllText( SAVE_PATH ), m_instance );
					}
					catch( System.Exception e )
					{
						Debug.LogException( e );
						m_instance = new Settings();
					}
				}

				return m_instance;
			}
		}

		public void Save()
		{
			File.WriteAllText( SAVE_PATH, EditorJsonUtility.ToJson( this, true ) );
		}

#if UNITY_2018_3_OR_NEWER
		[SettingsProvider]
		public static SettingsProvider CreatePreferencesGUI()
		{
			return new SettingsProvider( "Project/Player/Localized Title", SettingsScope.Project )
			{
				guiHandler = ( searchContext ) => PreferencesGUI(),
				keywords = new HashSet<string>() { "Localization", "Localized", "App", "Title", "Name", "Icon" }
			};
		}
#endif

#if !UNITY_2018_3_OR_NEWER
		[PreferenceItem( "Localized App Title" )]
#endif
		public static void PreferencesGUI()
		{
			Instance.DoPreferencesGUI();
		}

		private void DoPreferencesGUI()
		{
			if( localizedDataDrawer == null )
			{
				localizedDataDrawer = new ReorderableList( LocalizedData, typeof( LocalizedData ), true, false, true, true )
				{
					elementHeight = CalculateReorderableListHeight(),
					drawElementCallback = DrawLocalizedData,
					onRemoveCallback = ( list ) =>
					{
						LocalizedData.RemoveAt( list.index );
						DefaultLocalizedData = Mathf.Clamp( DefaultLocalizedData, 0, LocalizedData.Count - 1 );
					}
				};
			}

			EditorGUI.BeginChangeCheck(); // Check for any changes

			EditorGUI.BeginChangeCheck(); // Check for changes to localization toggles for any platform

			EditorGUI.BeginChangeCheck(); // Check for changes to Android platform localization toggles
			LocalizeAppNameOnAndroid = EditorGUILayout.ToggleLeft( "Localize App Name on Android", LocalizeAppNameOnAndroid );
			LocalizeAppIconOnAndroid = EditorGUILayout.ToggleLeft( "Localize App Icon on Android", LocalizeAppIconOnAndroid );
			if( EditorGUI.EndChangeCheck() && File.Exists( ANDROID_RESOURCES_LIBRARY_PATH ) )
			{
				AssetDatabase.DeleteAsset( ANDROID_RESOURCES_LIBRARY_PATH );
				Debug.Log( "(LocalizedAppTitle) Deleted auto-generated Android resources, you'll need to regenerate it: " + ANDROID_RESOURCES_LIBRARY_PATH );
			}

			EditorGUILayout.Space();

			LocalizeAppNameOniOS = EditorGUILayout.ToggleLeft( "Localize App Name on iOS", LocalizeAppNameOniOS );
			LocalizeAppIconOniOS = EditorGUILayout.ToggleLeft( localizeAppIconOniOSLabel, LocalizeAppIconOniOS );

			if( EditorGUI.EndChangeCheck() )
				localizedDataDrawer.elementHeight = CalculateReorderableListHeight();

			EditorGUI.BeginDisabledGroup( !LocalizeAppNameOnAndroid && !LocalizeAppIconOnAndroid && !LocalizeAppNameOniOS && !LocalizeAppIconOniOS );

			localizedDataDrawer.DoLayoutList();

			if( LocalizeAppNameOnAndroid || LocalizeAppIconOnAndroid )
			{
				EditorGUILayout.Space();

				if( LocalizeAppNameOnAndroid )
					AndroidNameResource = EditorGUILayout.DelayedTextField( androidNameResourceLabel, AndroidNameResource );
				if( LocalizeAppIconOnAndroid )
					AndroidIconResource = EditorGUILayout.DelayedTextField( androidIconResourceLabel, AndroidIconResource );
			}

			ReplaceApplicationProductName = EditorGUILayout.ToggleLeft( "Replace Application Product Name", ReplaceApplicationProductName );
			if( EditorGUI.EndChangeCheck() )
				Save();

			if( LocalizeAppNameOnAndroid || LocalizeAppIconOnAndroid )
			{
				EditorGUILayout.Space();

				if( GUILayout.Button( generateAndroidResourcesLabel ) )
					GenerateAndroidResources();
			}

			EditorGUI.EndDisabledGroup();
		}

		private void DrawLocalizedData( Rect rect, int index, bool isActive, bool isFocused )
		{
			rect.height = EditorGUIUtility.singleLineHeight;

			EditorGUI.BeginChangeCheck();
			bool isDefaultLocalizedData = EditorGUI.Toggle( rect, "Is Default", index == DefaultLocalizedData );
			if( EditorGUI.EndChangeCheck() && isDefaultLocalizedData )
				DefaultLocalizedData = index;

			rect.y += rect.height + SPACE_BETWEEN_INPUT_FIELDS;
			LocalizedData[index].LanguageCode = EditorGUI.DelayedTextField( rect, languageCodeLabel, LocalizedData[index].LanguageCode );

			if( LocalizeAppNameOnAndroid || LocalizeAppNameOniOS )
			{
				rect.y += rect.height + SPACE_BETWEEN_INPUT_FIELDS;
				LocalizedData[index].AppName = EditorGUI.DelayedTextField( rect, "App Name", LocalizedData[index].AppName );

				if( LocalizeAppNameOniOS )
				{
					EditorGUI.BeginChangeCheck();
					rect.y += rect.height + SPACE_BETWEEN_INPUT_FIELDS;
					LocalizedData[index].AppShortName = EditorGUI.DelayedTextField( rect, appShortNameLabel, LocalizedData[index].AppShortName );
					if( EditorGUI.EndChangeCheck() && LocalizedData[index].AppShortName.Length > 15 )
						LocalizedData[index].AppShortName = LocalizedData[index].AppShortName.Substring( 0, 15 );
				}
			}

			if( LocalizeAppIconOnAndroid )
			{
				rect.y += rect.height + SPACE_BETWEEN_INPUT_FIELDS;
				LocalizedData[index].AndroidIcon = EditorGUI.ObjectField( rect, "Android Icon", LocalizedData[index].AndroidIcon, typeof( Texture2D ), false ) as Texture2D;
			}

			if( LocalizeAppIconOniOS )
			{
				rect.y += rect.height + SPACE_BETWEEN_INPUT_FIELDS;
				LocalizedData[index].iOSIcon = EditorGUI.ObjectField( rect, "iOS Icon", LocalizedData[index].iOSIcon, typeof( Texture2D ), false ) as Texture2D;
			}
		}

		private float CalculateReorderableListHeight()
		{
			int rowCount = 1;
			if( LocalizeAppNameOnAndroid || LocalizeAppNameOniOS )
				rowCount += LocalizeAppNameOniOS ? 2 : 1;
			if( LocalizeAppIconOnAndroid )
				rowCount++;
			if( LocalizeAppIconOniOS )
				rowCount++;

			return EditorGUIUtility.singleLineHeight * ( rowCount + 1 ) + SPACE_BETWEEN_LIST_ELEMENTS + SPACE_BETWEEN_INPUT_FIELDS * rowCount;
		}

		private void GenerateAndroidResources()
		{
			Directory.CreateDirectory( Path.GetDirectoryName( ANDROID_RESOURCES_LIBRARY_PATH ) );

			string iconsTemporarySavePath = "Library/Icon_tmp.png";
			string[] iconsFolderNames = new string[] { "ldpi", "mdpi", "hdpi", "xhdpi", "xxhdpi", "xxxhdpi" };
			int[] iconsResolutions = new int[] { 36, 48, 72, 96, 144, 192 };

			HashSet<string> processedLanguages = new HashSet<string>();

			using( ZipStorer zip = ZipStorer.Create( ANDROID_RESOURCES_LIBRARY_PATH, "" ) )
			{
				// Each AAR library requires an AndroidManifest, create a dummy one
				string androidManifestContents =
					"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
					"<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"com.yasirkula.unity\">\n" +
					"    <uses-sdk android:targetSdkVersion=\"4\" />\n" +
					"</manifest>";

				zip.AddTextFile( ZipStorer.Compression.Deflate, "AndroidManifest.xml", androidManifestContents, System.DateTime.Now, "" );

				// Each AAR library requires a classes.jar file, create a dummy one
				ZipStorer.Create( iconsTemporarySavePath, "" ).Close();
				zip.AddFile( ZipStorer.Compression.Deflate, iconsTemporarySavePath, "classes.jar", "" );

				for( int i = 0; i < LocalizedData.Count; i++ )
				{
					string languageCode = LocalizedData[i].LanguageCode;
					int languageSubCodeIndex = languageCode.IndexOf( '-' );
					if( languageSubCodeIndex > 0 && languageCode[languageSubCodeIndex + 1] != 'r' )
					{
						languageCode = string.Concat( languageCode.Substring( 0, languageSubCodeIndex ), "-r", languageCode.Substring( languageSubCodeIndex + 1 ) );
						Debug.LogWarning( "(LocalizedAppTitle) Converted " + LocalizedData[i].LanguageCode + " to " + languageCode );
					}

					if( !processedLanguages.Add( languageCode ) )
					{
						Debug.LogWarning( "(LocalizedAppTitle) Language was already added, skipping: " + languageCode );
						continue;
					}

					if( LocalizeAppNameOnAndroid )
					{
						// Generate strings.xml files for localized app names
						string localizedStringsPath = ( i == DefaultLocalizedData ) ? "res/values/strings.xml" : ( "res/values-" + languageCode + "/strings.xml" );
						string localizedStringsContents =
							"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
							"<resources>\n" +
							"    <string name=\"" + AndroidNameResource + "\">" + LocalizedData[i].AppName + "</string>\n" +
							"</resources>";

						zip.AddTextFile( ZipStorer.Compression.Deflate, localizedStringsPath, localizedStringsContents, System.DateTime.Now, "" );
					}

					if( LocalizeAppIconOnAndroid )
					{
						// Generate localized icons
						string iconsDrawableFolderNamePrefix = ( i == DefaultLocalizedData ) ? "res/drawable-" : ( "res/drawable-" + languageCode + "-" );
						string iconsMipmapFolderNamePrefix = ( i == DefaultLocalizedData ) ? "res/mipmap-" : ( "res/mipmap-" + languageCode + "-" );
						for( int j = 0; j < iconsFolderNames.Length; j++ )
						{
							LocalizedData[i].AndroidIcon.SaveAs( iconsTemporarySavePath, iconsResolutions[j] );

							zip.AddFile( ZipStorer.Compression.Deflate, iconsTemporarySavePath, iconsDrawableFolderNamePrefix + iconsFolderNames[j] + "/" + AndroidIconResource + ".png", "" );
							zip.AddFile( ZipStorer.Compression.Deflate, iconsTemporarySavePath, iconsMipmapFolderNamePrefix + iconsFolderNames[j] + "/" + AndroidIconResource + ".png", "" );
						}
					}
				}
			}

			AssetDatabase.ImportAsset( ANDROID_RESOURCES_LIBRARY_PATH, ImportAssetOptions.ForceUpdate );

			Debug.Log( "(LocalizedAppTitle) Generated Android resources: " + ANDROID_RESOURCES_LIBRARY_PATH, AssetDatabase.LoadMainAssetAtPath( ANDROID_RESOURCES_LIBRARY_PATH ) );
		}
	}

	// Set Unity's 'Product Name' and 'Icon' to the default values specified in Settings and verify that all necessary fields are filled
#if UNITY_2018_1_OR_NEWER
	public class LocalizedAppTitlePreProcessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
#else
	public class LocalizedAppTitlePreProcessBuild : IPreprocessBuild, IPostprocessBuild
#endif
	{
		[System.Serializable]
		private class SerializedPlayerSettings
		{
#pragma warning disable 0649
			public bool LocalizedAppName;
			public string AppName;

			public bool LocalizedAppIcons;
			public Texture2D[] TargetPlatformIcons;
			public Texture2D[] UnknownPlatformIcons;
#pragma warning restore 0649
		}

		private const string SERIALIZED_PLAYER_SETTINGS_FILE = "Library/LocalizedAppTitle_PlayerSettings.json";

		int IOrderedCallback.callbackOrder { get { return 0; } }

#if UNITY_2018_1_OR_NEWER
		void IPreprocessBuildWithReport.OnPreprocessBuild( BuildReport report )
#else
		void IPreprocessBuild.OnPreprocessBuild( BuildTarget target, string path )
#endif
		{
			if( Settings.Instance.LocalizedData.Count == 0 )
				return;

#if UNITY_2018_1_OR_NEWER
			BuildTarget target = report.summary.platform;
#endif
			bool shouldLocalizeAppName = false, shouldLocalizeAppIcon = false;
			switch( target )
			{
				case BuildTarget.Android:
					shouldLocalizeAppName = Settings.Instance.LocalizeAppNameOnAndroid;
					shouldLocalizeAppIcon = Settings.Instance.LocalizeAppIconOnAndroid;
					break;
				case BuildTarget.iOS:
					shouldLocalizeAppName = Settings.Instance.LocalizeAppNameOniOS;
					shouldLocalizeAppIcon = Settings.Instance.LocalizeAppIconOniOS;
					break;
				default: return;
			}

			if( !shouldLocalizeAppName && !shouldLocalizeAppIcon )
				return;

			foreach( LocalizedData localizedData in Settings.Instance.LocalizedData )
			{
				if( string.IsNullOrEmpty( localizedData.LanguageCode ) )
					throw new System.Exception( "(LocalizedAppTitle) 'Language Code' isn't specified for a language" );
			}

			SerializedPlayerSettings serializedPlayerSettings = new SerializedPlayerSettings();

			if( shouldLocalizeAppName )
			{
				foreach( LocalizedData localizedData in Settings.Instance.LocalizedData )
				{
					if( string.IsNullOrEmpty( localizedData.AppName ) )
						throw new System.Exception( "(LocalizedAppTitle) 'App Name' isn't specified for language: " + localizedData.LanguageCode );
				}

				serializedPlayerSettings.LocalizedAppName = true;
				serializedPlayerSettings.AppName = PlayerSettings.productName;
			}

			if( shouldLocalizeAppIcon )
			{
				foreach( LocalizedData localizedData in Settings.Instance.LocalizedData )
				{
					Texture2D icon = ( target == BuildTarget.Android ) ? localizedData.AndroidIcon : localizedData.iOSIcon;
					if( !icon )
						throw new System.Exception( "(LocalizedAppTitle) 'Icon' isn't specified for language: " + localizedData.LanguageCode );
				}

				serializedPlayerSettings.LocalizedAppIcons = true;
				serializedPlayerSettings.TargetPlatformIcons = PlayerSettings.GetIconsForTargetGroup( target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS );
				serializedPlayerSettings.UnknownPlatformIcons = PlayerSettings.GetIconsForTargetGroup( BuildTargetGroup.Unknown );
			}

			// Store the previous PlayerSettings values in a temporary file so that they can be restored in OnPostprocessBuild (i.e. non-destructive workflow)
			File.WriteAllText( SERIALIZED_PLAYER_SETTINGS_FILE, EditorJsonUtility.ToJson( serializedPlayerSettings, false ) );

			if( shouldLocalizeAppName && Settings.Instance.ReplaceApplicationProductName)
				PlayerSettings.productName = Settings.Instance.LocalizedData[Settings.Instance.DefaultLocalizedData].AppName;

			if( shouldLocalizeAppIcon )
			{
				PlayerSettings.SetIconsForTargetGroup( target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS, new Texture2D[0] );
				PlayerSettings.SetIconsForTargetGroup( BuildTargetGroup.Unknown, new Texture2D[1] { Settings.Instance.LocalizedData[Settings.Instance.DefaultLocalizedData].AndroidIcon } ); // Credit: http://answers.unity.com/answers/1732975/view.html
			}
		}

#if UNITY_2018_1_OR_NEWER
		void IPostprocessBuildWithReport.OnPostprocessBuild( BuildReport report )
#else
		void IPostprocessBuild.OnPostprocessBuild( BuildTarget target, string path )
#endif
		{
			if( Settings.Instance.LocalizedData.Count == 0 )
				return;

#if UNITY_2018_1_OR_NEWER
			BuildTarget target = report.summary.platform;
#endif
			if( target != BuildTarget.Android && target != BuildTarget.iOS )
				return;

			if( File.Exists( SERIALIZED_PLAYER_SETTINGS_FILE ) )
			{
				SerializedPlayerSettings serializedPlayerSettings = new SerializedPlayerSettings();
				EditorJsonUtility.FromJsonOverwrite( File.ReadAllText( SERIALIZED_PLAYER_SETTINGS_FILE ), serializedPlayerSettings );
				File.Delete( SERIALIZED_PLAYER_SETTINGS_FILE );

				if( serializedPlayerSettings.LocalizedAppName && Settings.Instance.ReplaceApplicationProductName)
					PlayerSettings.productName = serializedPlayerSettings.AppName;
				if( serializedPlayerSettings.LocalizedAppIcons )
				{
					// PlayerSettings.GetIconsForTargetGroup returns an array filled with null values when "Override for {0}" option is disabled for target platform.
					// However, when we call SetIconsForTargetGroup with that array, "Override for {0}" option will remain enabled. We need to pass an empty array to disable that option
					if( serializedPlayerSettings.TargetPlatformIcons != null && !System.Array.Find( serializedPlayerSettings.TargetPlatformIcons, ( icon ) => icon ) )
						serializedPlayerSettings.TargetPlatformIcons = new Texture2D[0];

					PlayerSettings.SetIconsForTargetGroup( target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS, serializedPlayerSettings.TargetPlatformIcons );
					PlayerSettings.SetIconsForTargetGroup( BuildTargetGroup.Unknown, serializedPlayerSettings.UnknownPlatformIcons );
				}
			}
		}
	}

	public static class LocalizedAppTitlePostProcessBuild
	{
#if UNITY_IOS
		[PostProcessBuild( 1 )]
		public static void OnPostprocessBuild( BuildTarget target, string buildPath )
		{
			if( Settings.Instance.LocalizedData.Count == 0 )
				return;

			if( target == BuildTarget.iOS )
			{
				if( !Settings.Instance.LocalizeAppNameOniOS && !Settings.Instance.LocalizeAppIconOniOS )
					return;

				string pbxProjectPath = PBXProject.GetPBXProjectPath( buildPath );
				string plistPath = Path.Combine( buildPath, "Info.plist" );

				PBXProject pbxProject = new PBXProject();
				pbxProject.ReadFromFile( pbxProjectPath );

#if UNITY_2019_3_OR_NEWER
				string unityFrameworkGUID = pbxProject.GetUnityFrameworkTargetGuid();
				string mainTargetGUID = pbxProject.GetUnityMainTargetGuid();
#else
				string unityFrameworkGUID = pbxProject.TargetGuidByName( PBXProject.GetUnityTargetName() );
#endif

				LocalizedData defaultLocalizedData = Settings.Instance.LocalizedData[Settings.Instance.DefaultLocalizedData];

				string[] iconsFilenames = new string[] { "_iPhone@2x.png", "_iPhone@3x.png", "_iPad.png", "_iPad@2x.png", "_iPadPro@2x.png" };
				int[] iconsResolutions = new int[] { 120, 180, 76, 152, 167 };

				HashSet<string> processedLanguages = new HashSet<string>();

				foreach( LocalizedData localizedData in Settings.Instance.LocalizedData )
				{
					if( !processedLanguages.Add( localizedData.LanguageCode ) )
					{
						Debug.LogWarning( "(LocalizedAppTitle) Language was already added, skipping: " + localizedData.LanguageCode );
						continue;
					}

					if( Settings.Instance.LocalizeAppNameOniOS )
					{
						string localizationFolder = Path.Combine( buildPath, localizedData.LanguageCode + ".lproj" );
						string localizedPlistPath = Path.Combine( localizationFolder, "InfoPlist.strings" );

						// Create InfoPlist.strings files for localized app names
						string localizedPlistContents;
						if( File.Exists( localizedPlistPath ) )
						{
							localizedPlistContents = InsertValueToPlist( File.ReadAllText( localizedPlistPath ), "CFBundleDisplayName", localizedData.AppName );
							if( !string.IsNullOrEmpty( localizedData.AppShortName ) )
								localizedPlistContents = InsertValueToPlist( localizedPlistContents, "CFBundleName", localizedData.AppShortName );
						}
						else
						{
							Directory.CreateDirectory( localizationFolder );

							localizedPlistContents = "\"CFBundleDisplayName\" = \"" + localizedData.AppName + "\";";
							if( !string.IsNullOrEmpty( localizedData.AppShortName ) )
								localizedPlistContents += "\n\"CFBundleName\" = \"" + localizedData.AppShortName + "\";";
						}

						File.WriteAllText( localizedPlistPath, localizedPlistContents );

						// Add localization folder as reference to the project
						// Credit: https://forum.unity.com/threads/how-to-add-infoplist-strings-into-xcode-project-in-script.394488/#post-7046881
						string localizationFolderGuid = pbxProject.AddFolderReference( Path.GetFileName( localizationFolder ), Path.GetFileName( localizationFolder ), PBXSourceTree.Source );
						pbxProject.AddFileToBuild( unityFrameworkGUID, localizationFolderGuid );
#if UNITY_2019_3_OR_NEWER
						pbxProject.AddFileToBuild( mainTargetGUID, localizationFolderGuid );
#endif
					}

					if( Settings.Instance.LocalizeAppIconOniOS )
					{
						// Create localized icons
						// Credit: https://stackoverflow.com/questions/51949430/changing-alternate-icon-for-ipad
						// Credit (MIT-License): https://github.com/kyubuns/AppIconChangerUnity/blob/4c608cce17aa824f479a76386cc6008bdfd388f0/Assets/Plugins/AppIconChanger/Editor/PostProcesser.cs
						string iconsFolderRelativePath = "LocalizedIcon_" + localizedData.LanguageCode;
						string iconsFolderFullPath = Path.Combine( buildPath, iconsFolderRelativePath );
						Directory.CreateDirectory( iconsFolderFullPath );

						for( int i = 0; i < iconsFilenames.Length; i++ )
						{
							string iconFilename = "LocalizedIcon_" + localizedData.LanguageCode + iconsFilenames[i];
							string iconRelativePath = iconsFolderRelativePath + "/" + iconFilename;
							string iconFullPath = Path.Combine( iconsFolderFullPath, iconFilename );

							localizedData.iOSIcon.SaveAs( iconFullPath, iconsResolutions[i] );

							// Add icon files as reference to the project (adding only the folder as reference won't work, icon files must be added explicitly instead)
							string localizedIconGuid = pbxProject.AddFile( iconRelativePath, iconRelativePath, PBXSourceTree.Source );
							pbxProject.AddFileToBuild( unityFrameworkGUID, localizedIconGuid );
#if UNITY_2019_3_OR_NEWER
							pbxProject.AddFileToBuild( mainTargetGUID, localizedIconGuid );
#endif
						}
					}
				}

				processedLanguages.Clear();

				File.WriteAllText( pbxProjectPath, pbxProject.WriteToString() );

				PlistDocument plist = new PlistDocument();
				plist.ReadFromString( File.ReadAllText( plistPath ) );

				PlistElementDict rootDict = plist.root;

				if( Settings.Instance.LocalizeAppNameOniOS )
				{
					rootDict.SetString( "CFBundleDevelopmentRegion", defaultLocalizedData.LanguageCode );
					rootDict.SetString( "CFBundleDisplayName", defaultLocalizedData.AppName );
					rootDict.SetString( "CFBundleName", defaultLocalizedData.AppShortName );
					rootDict.SetBoolean( "LSHasLocalizedDisplayName ", true );

					// Define localized languages
					// Credit: // Credit: https://forum.unity.com/threads/appname-localization-for-macos.958702/
					PlistElementArray languagesArray = rootDict.GetOrCreateArray( "CFBundleLocalizations" );
					foreach( LocalizedData localizedData in Settings.Instance.LocalizedData )
					{
						if( processedLanguages.Add( localizedData.LanguageCode ) )
							languagesArray.AddStringIfNotExists( localizedData.LanguageCode );
					}

					processedLanguages.Clear();
				}

				if( Settings.Instance.LocalizeAppIconOniOS )
				{
					// Define localized icons
					// Credit (MIT-License): https://github.com/kyubuns/AppIconChangerUnity/blob/4c608cce17aa824f479a76386cc6008bdfd388f0/Assets/Plugins/AppIconChanger/Editor/PostProcesser.cs
					PlistElementDict bundleIconsDict = rootDict.GetOrCreateDict( "CFBundleIcons" );
					PlistElementDict bundleIconsDictIPad = rootDict.GetOrCreateDict( "CFBundleIcons~ipad" );
					PlistElementDict bundleAlternateIconsDict = bundleIconsDict.GetOrCreateDict( "CFBundleAlternateIcons" );
					PlistElementDict bundleAlternateIconsDictIPad = bundleIconsDictIPad.GetOrCreateDict( "CFBundleAlternateIcons" );

					bundleIconsDict.CreateDict( "CFBundlePrimaryIcon" ).CreateBundleIconFilesDict( "" );
					bundleIconsDictIPad.CreateDict( "CFBundlePrimaryIcon" ).CreateBundleIconFilesDict( "" );

					foreach( LocalizedData localizedData in Settings.Instance.LocalizedData )
					{
						if( !processedLanguages.Add( localizedData.LanguageCode ) )
							continue;

						bundleAlternateIconsDict.CreateDict( "LocalizedIcon_" + localizedData.LanguageCode ).CreateBundleIconFilesDict( "LocalizedIcon_" + localizedData.LanguageCode + "_iPhone" );
						bundleAlternateIconsDictIPad.CreateDict( "LocalizedIcon_" + localizedData.LanguageCode ).CreateBundleIconFilesDict( "LocalizedIcon_" + localizedData.LanguageCode + "_iPad", "LocalizedIcon_" + localizedData.LanguageCode + "_iPadPro" );
					}

					processedLanguages.Clear();
				}

				File.WriteAllText( plistPath, plist.WriteToString() );
			}
		}

		private static string InsertValueToPlist( string plistText, string key, string value )
		{
			int keyIndex = plistText.IndexOf( key );
			if( keyIndex < 0 )
			{
				if( plistText.Length > 0 )
					plistText += "\n";

				plistText += string.Format( "\"{0}\" = \"{1}\";", key, value );
			}
			else
			{
				int valueStartIndex = plistText.IndexOf( '=', keyIndex ) + 1;
				int valueEndIndex = plistText.IndexOf( '\n', valueStartIndex );
				if( valueEndIndex < 0 )
					plistText = plistText.Substring( 0, valueStartIndex ) + string.Format( " \"{0}\";", value );
				else
					plistText = plistText.Substring( 0, valueStartIndex ) + string.Format( " \"{0}\";", value ) + plistText.Substring( valueEndIndex );
			}

			return plistText;
		}

		private static PlistElementArray GetOrCreateArray( this PlistElementDict dict, string key )
		{
			PlistElement currentValue;
			if( dict.values.TryGetValue( key, out currentValue ) && currentValue is PlistElementArray )
				return (PlistElementArray) currentValue;
			else
				return dict.CreateArray( key );
		}

		private static PlistElementDict GetOrCreateDict( this PlistElementDict dict, string key )
		{
			PlistElement currentValue;
			if( dict.values.TryGetValue( key, out currentValue ) && currentValue is PlistElementDict )
				return (PlistElementDict) currentValue;
			else
				return dict.CreateDict( key );
		}

		private static void CreateBundleIconFilesDict( this PlistElementDict dict, params string[] iconFiles )
		{
			PlistElementArray bundleIconFiles = dict.CreateArray( "CFBundleIconFiles" );
			for( int i = 0; i < iconFiles.Length; i++ )
				bundleIconFiles.AddString( iconFiles[i] );

			dict.SetBoolean( "UIPrerenderedIcon", false );
		}

		private static void AddStringIfNotExists( this PlistElementArray array, string value )
		{
			foreach( PlistElement arrayElement in array.values )
			{
				if( arrayElement is PlistElementString && arrayElement.AsString() == value )
					return;
			}

			array.AddString( value );
		}
#endif
	}

	internal static class TextureScale
	{
		private const string DOWNSCALED_ICON_ASSET_TEMP_PATH = "Assets/LocalizedAppTitleScaledIcon.png";

		public static void SaveAs( this Texture icon, string path, int dimensions )
		{
#if UNITY_2018_3_OR_NEWER
			if( icon.width == dimensions && icon.height == dimensions && icon.isReadable && icon is Texture2D )
			{
				File.WriteAllBytes( path, ( (Texture2D) icon ).EncodeToPNG() );
				return;
			}
#endif

			try
			{
				Texture mitchellDownscaledIcon = DownscaleWithMitchell( icon, dimensions );
#if UNITY_2018_3_OR_NEWER
				if( mitchellDownscaledIcon.width == dimensions && mitchellDownscaledIcon.height == dimensions && mitchellDownscaledIcon.isReadable && mitchellDownscaledIcon is Texture2D )
				{
					File.WriteAllBytes( path, ( (Texture2D) mitchellDownscaledIcon ).EncodeToPNG() );
					return;
				}
#endif

				Texture2D scaledIcon = null;
				RenderTexture activeRT = RenderTexture.active;
				RenderTexture rt = RenderTexture.GetTemporary( dimensions, dimensions );
				try
				{
					Graphics.Blit( mitchellDownscaledIcon, rt );
					RenderTexture.active = rt;

					scaledIcon = new Texture2D( dimensions, dimensions, TextureFormat.RGBA32, false );
					scaledIcon.ReadPixels( new Rect( 0, 0, dimensions, dimensions ), 0, 0, false );
					scaledIcon.Apply( false, false );

					File.WriteAllBytes( path, scaledIcon.EncodeToPNG() );
				}
				finally
				{
					RenderTexture.active = activeRT;
					RenderTexture.ReleaseTemporary( rt );

					if( scaledIcon )
						Object.DestroyImmediate( scaledIcon );
				}
			}
			finally
			{
				if( File.Exists( DOWNSCALED_ICON_ASSET_TEMP_PATH ) )
					AssetDatabase.DeleteAsset( DOWNSCALED_ICON_ASSET_TEMP_PATH );
			}
		}

		// Using Unity's Mitchell algorithm produces the smoothest downscaled icons among all Texture downscale solutions out there
		private static Texture DownscaleWithMitchell( this Texture icon, int dimensions )
		{
			if( icon.width <= dimensions && icon.height <= dimensions )
				return icon;

			if( AssetDatabase.IsMainAsset( icon ) )
			{
				if( !AssetDatabase.CopyAsset( AssetDatabase.GetAssetPath( icon ), DOWNSCALED_ICON_ASSET_TEMP_PATH ) )
					return icon;
			}
#if UNITY_2017_2_OR_NEWER
			else if( !string.IsNullOrEmpty( AssetDatabase.ExtractAsset( icon, DOWNSCALED_ICON_ASSET_TEMP_PATH ) ) )
#else
			else
#endif
			{
				return icon;
			}

			TextureImporter textureImporter = AssetImporter.GetAtPath( DOWNSCALED_ICON_ASSET_TEMP_PATH ) as TextureImporter;
			textureImporter.maxTextureSize = dimensions;
			textureImporter.npotScale = TextureImporterNPOTScale.None;
			textureImporter.wrapMode = TextureWrapMode.Clamp;
			textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
			textureImporter.isReadable = true;
			textureImporter.SaveAndReimport();

			return AssetDatabase.LoadAssetAtPath<Texture>( DOWNSCALED_ICON_ASSET_TEMP_PATH );
		}
	}
}