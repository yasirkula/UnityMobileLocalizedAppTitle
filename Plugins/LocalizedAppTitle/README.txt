= Localized App Title for Android & iOS (v1.0.1) =

Online documentation available at: https://github.com/yasirkula/UnityMobileLocalizedAppTitle
E-mail: yasirkula@gmail.com


1. ABOUT
This plugin helps you localize your app's name and/or icon on Android & iOS. Note that the icon doesn't automatically change with the device language on iOS but it's possible to change it at runtime via scripting API.


2. HOW TO
Simply open "Project Settings/Player/Localized Title" page (on older versions, it's located at "Preferences/Localized App Title") and tweak the settings as you wish:
- the default language will be used when none of the provided localizations match the device's language and on iOS, the default language's icon will be used by default
- for Android support, "Language Code" mustn't include language sub-code (i.e. use 'en' instead of 'en-US')
- on Android, after all values are set, you must click the "Generate Android Resources" button at the bottom (when a setting is changed, repeat the process)
- on iOS, it's unclear where "App Short Name" is used or whether or not it's used at all. It corresponds to CFBundleName and its value can be left empty
- In Player Settings, if you provide custom icons for "Adaptive icons", "Round icons", "Spotlight icons", "Settings icons" and etc., these icons will probably not be localized. So you should either not localize the app icon or clear these custom icons


3. SCRIPTING API
// These functions don't have any effect on Android

// Changes the app's icon to the specified language's icon (pass 'null' to revert to the default language's icon)
// Note that Apple may reject your app if you call this function without user input or you don't provide a way to revert to the default icon (for the latter case, passing 'null' instead of the default language's "Language Code" might be sufficient)
void LocalizedAppTitle.SetLanguage( string languageCode );

// Returns the current icon's "Language Code" or null if the default icon is used
string LocalizedAppTitle.GetLanguage();

// Returns true if SetLanguage and GetLanguage functions are supported (iOS 10.3 or later)
bool LocalizedAppTitle.SupportsSettingLanguage();