/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@class Mod;
@interface GameInstall : NSObject {
	NSURL *gameURL;
	NSMutableDictionary *downloadTasks;
}
@property(readonly) NSURL *gameURL;

-(id)initWithURL:(NSURL *)path;
-(void)launchMod:(NSString *)mod;
- (NSString *)runUtilityQuery:(NSString *)arg;
- (NSArray *)installedMods;
- (NSDictionary *)infoForMods:(NSArray *)mods;
- (BOOL)downloadUrl:(NSString *)url toPath:(NSString *)filename withId:(NSString *)key;
@end
