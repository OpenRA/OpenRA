/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "JSBridge.h"
#import "Controller.h"
#import "Download.h"
#import "Mod.h"

static JSBridge *SharedInstance;

@implementation JSBridge
@synthesize methods;

+ (JSBridge *)sharedInstance
{
	if (SharedInstance == nil)
		SharedInstance = [[JSBridge alloc] init];
	
	return SharedInstance;
}

+ (NSString *)webScriptNameForSelector:(SEL)sel
{
	return [[[JSBridge sharedInstance] methods] objectForKey:NSStringFromSelector(sel)];
}

+ (BOOL)isSelectorExcludedFromWebScript:(SEL)sel
{ 
	return [[[JSBridge sharedInstance] methods] objectForKey:NSStringFromSelector(sel)] == nil;
}

-(id)init
{
	self = [super init];
	if (self != nil)
	{
		methods = [[NSDictionary dictionaryWithObjectsAndKeys:
						@"launchMod", NSStringFromSelector(@selector(launchMod:)),
						@"log", NSStringFromSelector(@selector(log:)),
						@"fileExistsInMod", NSStringFromSelector(@selector(fileExists:inMod:)),
						@"fileExistsInCache", NSStringFromSelector(@selector(fileExistsInCache:)),
						@"downloadFileToCache", NSStringFromSelector(@selector(downloadFileIntoCache:withName:key:)),
						@"cancelDownload", NSStringFromSelector(@selector(cancelDownload:)),
					nil] retain];
	}
	return self;
}

- (void)setController:(Controller *)aController
{
	controller = [aController retain];
}

- (void)dealloc
{
	[controller release]; controller = nil;
	[super dealloc];
}

- (void)notifyDownloadProgress:(Download *)download
{
	NSLog(@"notified");
	//[[[controller webView] windowScriptObject] evaluateWebScript:
	// @"updateDownloadStatus(’sample_graphic.jpg’, ‘320’, ‘240’)"];
}

#pragma mark JS API methods

- (BOOL)launchMod:(NSString *)aMod
{
	// Build the list of mods to launch
	NSMutableArray *mods = [NSMutableArray array];
	NSString *current = aMod;
	
	// Assemble the mods in the reverse order to work around an engine bug
	while (current != nil)
	{
		Mod *mod = [[controller allMods] objectForKey:current];
		if (mod == nil)
		{
			NSLog(@"Unknown mod: %@", current);
			return NO;
		}
		[mods addObject:current];
		
		if ([mod standalone])
			current = nil;
		else
			current = [mod requires];
	}
	// Todo: Reverse the array ordering once the engine bug is fixed
	
	[controller launchMod:[mods componentsJoinedByString:@","]];
	return YES;
}

- (BOOL)fileExistsInCache:(NSString *)name
{
	// Disallow traversing directories; take only the last component
	id path = [[@"~/Library/Application Support/OpenRA/Downloads/" stringByAppendingPathComponent:[name lastPathComponent]] stringByExpandingTildeInPath];
	return [[NSFileManager defaultManager] fileExistsAtPath:path];
}

- (void)downloadFileIntoCache:(NSString *)url withName:(NSString *)name key:(NSString *)key
{
	NSLog(@"downloadFile:%@ intoCacheWithName:%@ key:%@",url,name,key);
	
	// Disallow traversing directories; take only the last component
	id path = [[@"~/Library/Application Support/OpenRA/Downloads/" stringByAppendingPathComponent:[name lastPathComponent]] stringByExpandingTildeInPath];
	[controller downloadUrl:url toFile:path withId:key];
}

- (void)cancelDownload:(NSString *)key
{
	[controller cancelDownload:key];
}

- (void)log:(NSString *)message
{
	NSLog(@"js: %@",message);
}

- (BOOL)fileExists:(NSString *)aFile inMod:(NSString *)aMod
{
	id mod = [[controller allMods] objectForKey:aMod];
	if (mod == nil)
	{
		NSLog(@"Invalid or unknown mod: %@", aMod);
		return NO;
	}
	
	// Disallow traversing up the directory tree
	id path = [[[mod baseURL] absoluteString]
			   stringByAppendingPathComponent:[aFile stringByReplacingOccurrencesOfString:@"../"
																			   withString:@""]];
	
	return [[NSFileManager defaultManager] fileExistsAtPath:path];
}

@end
