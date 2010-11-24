//
//  Download.h
//  OpenRA
//
//  Created by Paul Chote on 19/11/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

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

@end