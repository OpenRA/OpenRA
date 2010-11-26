/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>

@class GameInstall;
@interface Download : NSObject
{
	NSString *key;
	NSString *url;
	NSString *filename;
	GameInstall *game;
	NSTask *task;
	NSString *status;
	NSString *error;
	int bytesCompleted;
	int bytesTotal;
}

@property(readonly) NSString *key;
@property(readonly) NSString *status;
@property(readonly) int bytesCompleted;
@property(readonly) int bytesTotal;
@property(readonly) NSString *error;

+ (id)downloadWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)aGame;
- (id)initWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)game;
- (BOOL)start;
- (BOOL)cancel;
- (BOOL)extractToPath:(NSString *)aPath;
@end