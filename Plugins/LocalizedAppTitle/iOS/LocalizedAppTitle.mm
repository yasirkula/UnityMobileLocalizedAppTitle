#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

#define CHECK_IOS_VERSION( version )  ([[[UIDevice currentDevice] systemVersion] compare:version options:NSNumericSearch] != NSOrderedAscending)

extern "C" int _LocalizedAppTitle_SupportsLocalizedIcons() 
{
	return ( CHECK_IOS_VERSION( @"10.3" ) && [[UIApplication sharedApplication] supportsAlternateIcons] ) ? 1 : 0;
}

extern "C" char* _LocalizedAppTitle_GetLanguage() 
{
	NSString *alternateIconName = ( _LocalizedAppTitle_SupportsLocalizedIcons() == 1 ) ? [[UIApplication sharedApplication] alternateIconName] : @"";
	if( alternateIconName == nil )
		alternateIconName = @"";
	
	const char *languageUTF8 = [alternateIconName UTF8String];
	char *result = (char*) malloc( strlen( languageUTF8 ) + 1 );
	strcpy( result, languageUTF8 );
	
	return result;
}

extern "C" void _LocalizedAppTitle_SetLanguage( const char* languageCode ) 
{
	NSString *languageCodeStr = [NSString stringWithUTF8String:languageCode];
	
	// Credit: https://stackoverflow.com/a/9939963/2373034
	// Change app's language
	if( [languageCodeStr length] > 0 )
		[[NSUserDefaults standardUserDefaults] setObject:[NSArray arrayWithObjects:languageCodeStr, nil] forKey:@"AppleLanguages"];
	else
		[[NSUserDefaults standardUserDefaults] removeObjectForKey:@"AppleLanguages"];
	
	[[NSUserDefaults standardUserDefaults]synchronize];
	
	// Change app's localized icon
	if( _LocalizedAppTitle_SupportsLocalizedIcons() == 1 )
	{
		NSString *currentAlternateIconName = [[UIApplication sharedApplication] alternateIconName];
		NSString *newAlternateIconName = [languageCodeStr length] > 0 ? [NSString stringWithFormat:@"LocalizedIcon_%@", languageCodeStr] : nil;
		
		// Don't attempt to change the icon if the target icon is already in use (this way, we won't be showing an icon-change notification for no reason)
		if( ( currentAlternateIconName == nil && newAlternateIconName != nil ) || ( currentAlternateIconName != nil && ![currentAlternateIconName isEqual:newAlternateIconName] ) )
		{
			[[UIApplication sharedApplication] setAlternateIconName:newAlternateIconName completionHandler:^( NSError *error )
			{
				if( error != nil )
					NSLog( @"(LocalizedAppTitle) Couldn't set localized app icon: %@", error );
			}];
		}
	}
}