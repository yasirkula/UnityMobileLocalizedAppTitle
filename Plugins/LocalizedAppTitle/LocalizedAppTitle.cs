using UnityEngine;

public static class LocalizedAppTitle
{
#if !UNITY_EDITOR && UNITY_IOS
	[System.Runtime.InteropServices.DllImport( "__Internal" )]
	private static extern int _LocalizedAppTitle_SupportsLocalizedIcons();

	[System.Runtime.InteropServices.DllImport( "__Internal" )]
	private static extern string _LocalizedAppTitle_GetLanguage();

	[System.Runtime.InteropServices.DllImport( "__Internal" )]
	private static extern void _LocalizedAppTitle_SetLanguage( string languageCode );
#endif

	public static bool SupportsSettingLanguage()
	{
#if !UNITY_EDITOR && UNITY_IOS
		return _LocalizedAppTitle_SupportsLocalizedIcons() == 1;
#else
		return false;
#endif
	}

	public static string GetLanguage()
	{
#if !UNITY_EDITOR && UNITY_IOS
		string language = _LocalizedAppTitle_GetLanguage();
		return ( language.Length > 0 && language.StartsWith( "LocalizedIcon_" ) ) ? language.Substring( "LocalizedIcon_".Length ) : null;
#else
		return null;
#endif
	}

	public static void SetLanguage( string languageCode )
	{
		if( languageCode == null )
			languageCode = "";

#if !UNITY_EDITOR && UNITY_IOS
		Debug.Log( "App title language set to: " + languageCode );
		_LocalizedAppTitle_SetLanguage( languageCode );
#endif
	}
}