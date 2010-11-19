//
//  Download.m
//  OpenRA
//
//  Created by Paul Chote on 19/11/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "Download.h"
#import "GameInstall.h"
#import "JSBridge.h"

@implementation Download

+ (id)downloadWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)aGame
{
	id newObject = [[self alloc] initWithURL:aURL filename:aFilename key:aKey game:aGame];
	[newObject autorelease];
	return newObject;
}

- (id)initWithURL:(NSString *)aURL filename:(NSString *)aFilename key:(NSString *)aKey game:(GameInstall *)aGame;
{
	self = [super init];
	if (self != nil)
	{
		url = [aURL retain];
		filename = [aFilename retain];
		key = [aKey retain];
		game = [aGame retain];
		
		NSLog(@"Starting download...");
		task = [game runAsyncUtilityWithArg:[NSString stringWithFormat:@"--download-url=%@,%@",url,filename]
								   delegate:self
						   responseSelector:@selector(utilityResponded:)
						 terminatedSelector:@selector(utilityTerminated:)];
		[task retain];
	}
	return self;
}

- (void)cancel
{
	// Stop the download task. utilityTerminated: will handle the cleanup
	NSLog(@"Cancelling");
	NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
	[nc removeObserver:self name:NSFileHandleReadCompletionNotification object:[[task standardOutput] fileHandleForReading]];
	[nc removeObserver:self name:NSTaskDidTerminateNotification object:task];
	[task terminate];
}

- (void)utilityResponded:(NSNotification *)n
{
	NSData *data = [[n userInfo] valueForKey:NSFileHandleNotificationDataItem];
	NSString *response = [[[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding] autorelease];
	NSLog(@"r: %@",response);
	
	[[JSBridge sharedInstance] notifyDownloadProgress:self];
		
	// Keep reading
	if ([n object] != nil)
		[[n object] readInBackgroundAndNotify];
}

- (void)utilityTerminated:(NSNotification *)n
{
	NSLog(@"utility terminated");
	NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
	[nc removeObserver:self name:NSFileHandleReadCompletionNotification object:[[task standardOutput] fileHandleForReading]];
	[nc removeObserver:self name:NSTaskDidTerminateNotification object:task];
	[task release]; task = nil;
}

@end
