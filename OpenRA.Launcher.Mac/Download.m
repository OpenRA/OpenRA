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
@synthesize key;
@synthesize status;
@synthesize bytesCompleted;
@synthesize bytesTotal;

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
		
		if ([[NSFileManager defaultManager] fileExistsAtPath:filename])
		{
			status = @"COMPLETE";
			bytesCompleted = bytesTotal = [[[NSFileManager defaultManager] attributesOfItemAtPath:filename error:nil] fileSize];
		}
		else
		{
			status = @"AVAILABLE";
			bytesCompleted = -1;
			bytesTotal = -1;
		}
	}
	return self;
}

- (void)utilityResponded:(NSNotification *)n
{
	NSData *data = [[n userInfo] valueForKey:NSFileHandleNotificationDataItem];
	NSString *response = [[[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding] autorelease];
	
	// Response can contain multiple lines, or no lines. Split into lines, and parse each in turn
	NSArray *lines = [response componentsSeparatedByString:@"\n"];
	for (NSString *line in lines)
	{
		NSRange separator = [line rangeOfString:@":"];
		if (separator.location == NSNotFound)
			continue; // We only care about messages of the form key: value
		
		NSString *type = [line substringToIndex:separator.location];
		NSString *message = [[line substringFromIndex:separator.location+1] 
							 stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
		
		if ([type isEqualToString:@"Error"])
		{
			status = @"ERROR";
		}
		else if ([type isEqualToString:@"Status"])
		{
			if ([message isEqualToString:@"Completed"])
			{
				status = @"COMPLETE";
			}
			
			// Parse download status info
			int done,total;
			if (sscanf([message UTF8String], "%*d%% %d/%d bytes", &done, &total) == 2)
			{
				bytesCompleted = done;
				bytesTotal = total;
			}
		}
	}
	[[JSBridge sharedInstance] notifyDownloadProgress:self];
		
	// Keep reading
	if ([n object] != nil)
		[[n object] readInBackgroundAndNotify];
}

- (BOOL)start
{
	status = @"DOWNLOADING";

	NSLog(@"Starting download...");
	task = [game runAsyncUtilityWithArg:[NSString stringWithFormat:@"--download-url=%@,%@",url,filename]
							   delegate:self
					   responseSelector:@selector(utilityResponded:)
					 terminatedSelector:@selector(utilityTerminated:)];
	[task retain];
	return YES;
}

- (BOOL)cancel
{
	NSLog(@"Cancelling");
	status = @"ERROR";
	NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
	[nc removeObserver:self name:NSFileHandleReadCompletionNotification object:[[task standardOutput] fileHandleForReading]];
	[nc removeObserver:self name:NSTaskDidTerminateNotification object:task];
	[task terminate];
	[task release]; task = nil;
	return YES;
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
