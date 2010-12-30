/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "HttpRequest.h"
#import "JSBridge.h"
#import "GameInstall.h"

@implementation HttpRequest
@synthesize url;

+ (id)requestWithURL:(NSString *)aURL callback:(NSString *)aCallback game:(GameInstall *)aGame
{
	id newObject = [[self alloc] initWithURL:aURL callback:aCallback game:aGame];
	[newObject autorelease];
	return newObject;
}

- (id)initWithURL:(NSString *)aURL callback:(NSString *)aCallback game:(GameInstall *)aGame;
{
	self = [super init];
	if (self != nil)
	{
		NSLog(@"Requesting url `%@` with callback:`%@`",aURL, aCallback);
		url = [aURL retain];
		callback = [aCallback retain];
		game = [aGame retain];
		response = [[NSMutableData alloc] init];
		
		task = [game runAsyncUtilityWithArg:[NSString stringWithFormat:@"--download-url=%@",url]
								   delegate:self
						   responseSelector:@selector(utilityResponded:)
						 terminatedSelector:@selector(utilityTerminated:)];
		[task retain];
	}
	return self;
}

- (void)cancel
{
	cancelled = YES;
	[[NSNotificationCenter defaultCenter] removeObserver:self
													name:NSFileHandleReadCompletionNotification
												  object:[[task standardOutput] fileHandleForReading]];
	[task terminate];
}

- (void)utilityResponded:(NSNotification *)n
{
	NSData *data = [[n userInfo] valueForKey:NSFileHandleNotificationDataItem];
	[response appendData:data];
	
	// Keep reading
	if ([n object] != nil)
		[[n object] readInBackgroundAndNotify];
}

- (void)utilityTerminated:(NSNotification *)n
{
	NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
	[nc removeObserver:self name:NSFileHandleReadCompletionNotification object:[[task standardOutput] fileHandleForReading]];
	[nc removeObserver:self name:NSTaskDidTerminateNotification object:task];
	[task release]; task = nil;
	
	if (!cancelled)
	{
		NSString *data = [[[NSString alloc] initWithData:response encoding:NSASCIIStringEncoding] autorelease];
		[[JSBridge sharedInstance] runCallback:callback withArgument:data];
	}
}

- (BOOL)terminated
{
	return task == nil;
}

@end
