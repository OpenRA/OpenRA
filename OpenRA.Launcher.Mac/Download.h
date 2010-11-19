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
}
+ (id)downloadWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)aGame;
- (id)initWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)game;
- (void)cancel;

@end