//
//  Download.h
//  OpenRA
//
//  Created by Paul Chote on 19/11/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Cocoa/Cocoa.h>

typedef enum {
	Initializing,
	Downloading,
	Complete,
	Cancelled,
	Error
} DownloadStatus;

@class GameInstall;
@interface Download : NSObject
{
	NSString *key;
	NSString *url;
	NSString *filename;
	GameInstall *game;
	NSTask *task;
	DownloadStatus status;
	int bytesCompleted;
	int bytesTotal;
}
@property(readonly) NSString *key;
@property(readonly) DownloadStatus status;
@property(readonly) int bytesCompleted;
@property(readonly) int bytesTotal;

+ (id)downloadWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)aGame;
- (id)initWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)game;
- (void)cancel;

@end