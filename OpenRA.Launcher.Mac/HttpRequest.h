/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@class WebScriptObject;
@class GameInstall;
@interface HttpRequest : NSObject
{
	NSString *url;
	NSString *callback;
	GameInstall *game;
	NSTask *task;
	NSMutableData *response;
	BOOL cancelled;
}
@property(readonly) NSString *url;

+ (id)requestWithURL:(NSString *)aURL callback:(NSString *)aCallback game:(GameInstall *)aGame;
- (id)initWithURL:(NSString *)aURL callback:(NSString *)aCallback game:(GameInstall *)aGame;
- (void)cancel;
- (BOOL)terminated;

@end
