/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "Download.h"
#import "GameInstall.h"
#import "JSBridge.h"

@implementation Download
@synthesize key;
@synthesize status;
@synthesize bytesCompleted;
@synthesize bytesTotal;
@synthesize error;

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
		error = @"";
		
		if ([[NSFileManager defaultManager] fileExistsAtPath:filename])
		{
			status = @"DOWNLOADED";
			bytesCompleted = bytesTotal = [[[NSFileManager defaultManager] attributesOfItemAtPath:filename error:NULL] fileSize];
		}
		else
		{
			status = @"AVAILABLE";
			bytesCompleted = bytesTotal = -1;
		}
	}
	return self;
}


- (BOOL)start
{
	status = @"DOWNLOADING";
	task = [game runAsyncUtilityWithArg:[NSString stringWithFormat:@"--download-url=%@,%@",url,filename]
							   delegate:self
					   responseSelector:@selector(downloadResponded:)
					 terminatedSelector:@selector(utilityTerminated:)];
	[task retain];
	return YES;
}

- (BOOL)cancel
{
	status = @"ERROR";
	error = @"Download Cancelled";
	
	[[NSFileManager defaultManager] removeItemAtPath:filename error:NULL];
	bytesCompleted = bytesTotal = -1;
	
	[[JSBridge sharedInstance] notifyDownloadProgress:self];
	[[NSNotificationCenter defaultCenter] removeObserver:self
													name:NSFileHandleReadCompletionNotification
												  object:[[task standardOutput] fileHandleForReading]];
	[task terminate];
	return YES;
}

- (void)downloadResponded:(NSNotification *)n
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
			[error autorelease];
			if ([[message substringToIndex:36] isEqualToString:@"The remote server returned an error:"])
				error = [[message substringFromIndex:37] retain];
			else
				error = [message retain];
		}
			
		else if ([type isEqualToString:@"Status"])
		{
			if ([message isEqualToString:@"Completed"])
			{
				status = @"DOWNLOADED";
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

- (BOOL)extractToPath:(NSString *)aPath
{
	status = @"EXTRACTING";
	task = [game runAsyncUtilityWithArg:[NSString stringWithFormat:@"--extract-zip=%@,%@",filename,aPath]
							   delegate:self
					   responseSelector:@selector(extractResponded:)
					 terminatedSelector:@selector(utilityTerminated:)];
	[task retain];
	return YES;
}

- (void)extractResponded:(NSNotification *)n
{
	NSData *data = [[n userInfo] valueForKey:NSFileHandleNotificationDataItem];
	NSString *response = [[[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding] autorelease];
	
	// Response can contain multiple lines, or no lines. Split into lines, and parse each in turn
	NSArray *lines = [response componentsSeparatedByString:@"\n"];
	for (NSString *line in lines)
	{
		NSLog(@"%@",line);
		NSRange separator = [line rangeOfString:@":"];
		if (separator.location == NSNotFound)
			continue; // We only care about messages of the form key: value
		
		NSString *type = [line substringToIndex:separator.location];
		NSString *message = [[line substringFromIndex:separator.location+1] 
							 stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
		
		if ([type isEqualToString:@"Error"])
		{
			status = @"ERROR";
			[error autorelease];
			error = [message retain];
		}
		
		else if ([type isEqualToString:@"Status"])
		{
			if ([message isEqualToString:@"Completed"])
			{
				status = @"EXTRACTED";
			}
		}
	}
	[[JSBridge sharedInstance] notifyExtractProgress:self];
	
	// Keep reading
	if ([n object] != nil)
		[[n object] readInBackgroundAndNotify];
}

- (void)utilityTerminated:(NSNotification *)n
{
	NSLog(@"download terminated");
	NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
	[nc removeObserver:self name:NSFileHandleReadCompletionNotification object:[[task standardOutput] fileHandleForReading]];
	[nc removeObserver:self name:NSTaskDidTerminateNotification object:task];
	[task release]; task = nil;
	
	if (status == @"ERROR")
	{	
		[[NSFileManager defaultManager] removeItemAtPath:filename error:NULL];
		bytesCompleted = bytesTotal = -1;
	}
}

@end
