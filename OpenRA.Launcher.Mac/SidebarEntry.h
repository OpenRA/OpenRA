/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@class Mod;
@interface SidebarEntry : NSObject
{
	BOOL isHeader;
	NSString *title;
	NSImage *icon;
	id object;
	NSMutableArray *children;
}

@property (readonly) BOOL isHeader;
@property (readonly) NSString *title;
@property (readonly) NSMutableArray* children;
@property (readonly) NSImage* icon;

+ (id)headerWithTitle:(NSString *)aTitle;
+ (id)entryWithTitle:(NSString *)aTitle object:(id)anObject icon:(id)anIcon;
+ (id)entryWithMod:(Mod *)baseMod allMods:(NSArray *)allMods;
- (id)initWithTitle:(NSString *)aTitle object:(id)anObject icon:(id)anIcon isHeader:(BOOL)aHeader;
- (void)addChild:(id)child;

@end
