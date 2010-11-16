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
	NSString *gamePath;
	NSMutableString *utilityBuffer;
}

-(id)initWithPath:(NSString *)path;
-(void)launchGame;
- (void)runUtilityApp:(NSString *)arg handleOutput:(id)obj withMethod:(SEL)sel;
- (NSArray *)installedMods;
- (NSArray *)infoForMods:(NSArray *)mods;
@end
