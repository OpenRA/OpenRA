/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@interface Mod : NSObject {
	NSString *path;
	NSString *mod;
	NSString *title;
	NSString *version;
	NSString *author;
	NSString *requires;
	NSString *description;
	BOOL standalone;
}

@property (readonly) NSString *mod;
@property (readonly) NSString *title;
@property (readonly) NSString *version;
@property (readonly) NSString *author;
@property (readonly) NSString *description;
@property (readonly) NSString *requires;
@property (readonly) NSString *path;
@property (readonly) BOOL standalone;

+ (id)modWithId:(NSString *)mid fields:(id)fields path:(NSString *)path;
- (id)initWithId:(NSString *)anId fields:(NSDictionary *)fields path:(NSString *)path;
@end
