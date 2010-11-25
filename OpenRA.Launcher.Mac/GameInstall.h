/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@class Mod;
@class Controller;
@interface GameInstall : NSObject {
	NSString *gamePath;
	Controller *controller;
	NSMutableDictionary *downloadTasks;
}
@property(readonly) NSString *gamePath;

-(id)initWithPath:(NSString *)path;
-(void)launchMod:(NSString *)mod;
- (NSString *)runUtilityQuery:(NSString *)arg;
- (NSArray *)installedMods;
- (NSDictionary *)infoForMods:(NSArray *)mods;
- (NSTask *)runAsyncUtilityWithArg:(NSString *)arg 
						  delegate:(id)object
				  responseSelector:(SEL)response
				terminatedSelector:(SEL)terminated;
@end
