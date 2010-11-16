/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@interface ModEntry : NSObject {
	BOOL isHeader;
	NSString *mod;
	NSString *title;
	NSString *version;
	NSString *author;
	NSString *requires;
	BOOL standalone;
	NSMutableArray *children;
	NSImage *icon;
}

@property (readonly) BOOL isHeader;
@property (readonly) NSString *mod;
@property (readonly) NSString *title;
@property (readonly) NSString *version;
@property (readonly) NSString *author;
@property (readonly) NSString *description;
@property (readonly) NSString *requires;
@property (readonly) BOOL standalone;
@property (readonly) NSMutableArray* children;
@property (readonly) NSImage* icon;

+ (id)headerWithTitle:(NSString *)aTitle;
+ (id)errorWithTitle:(NSString *)aTitle;
+ (id)modWithId:(NSString *)mid fields:(id)fields;
- (id)initWithId:(NSString *)mod fields:(NSDictionary *)fields isHeader:(BOOL)header;
- (void)addChild:(id)child;
- (void)buildChildTree:(NSArray *)allMods;
- (id)icon;
@end
