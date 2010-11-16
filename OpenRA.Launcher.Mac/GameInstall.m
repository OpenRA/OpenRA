/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "GameInstall.h"
#import "Mod.h"

@implementation GameInstall
@synthesize gameURL;

-(id)initWithURL:(NSURL *)url
{
	self = [super init];
	if (self != nil)
	{
		gameURL = [url retain];
	}
	return self;
}

- (void)dealloc
{
	[gameURL release]; gameURL = nil;
	[super dealloc];
}

- (NSArray *)installedMods
{
	id raw = [self runUtilityQuery:@"-l"];
	id mods = [raw stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
	return [mods componentsSeparatedByString:@"\n"];
}

- (NSArray *)infoForMods:(NSArray *)mods
{
	id query = [NSString stringWithFormat:@"-i=%@",[mods componentsJoinedByString:@","]];
	NSArray *lines = [[self runUtilityQuery:query] componentsSeparatedByString:@"\n"];
	
	NSMutableArray *ret = [NSMutableArray array];
	NSMutableDictionary *fields = nil;
	NSString *current = nil;
	for (id l in lines)
	{
		id line = [l stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
		if (line == nil || [line length] == 0)
			continue;
		
		id kv = [line componentsSeparatedByString:@":"];
		if ([kv count] < 2)
			continue;
		
		id key = [kv objectAtIndex:0];
		id value = [kv objectAtIndex:1];
		
		if ([key isEqualToString:@"Error"])
		{	
			NSLog(@"Error: %@",value);
			continue;
		}
		
		if ([key isEqualToString:@"Mod"])
		{
			// Commit prev mod
			if (current != nil)
			{	
				id url = [gameURL URLByAppendingPathComponent:[NSString stringWithFormat:@"mods/%@",current]];
				[ret addObject:[Mod modWithId:current fields:fields baseURL:url]];
			}
			NSLog(@"Parsing mod %@",value);
			current = value;
			fields = [NSMutableDictionary dictionary];			
		}
		
		if (fields != nil)
			[fields setObject:[value stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]]
					   forKey:key];
	}
	if (current != nil)
	{	
		id url = [gameURL URLByAppendingPathComponent:[NSString stringWithFormat:@"mods/%@",current]];
		[ret addObject:[Mod modWithId:current fields:fields baseURL:url]];
	}
	
	return ret;
}

-(void)launchGame
{
	// Use LaunchServices because neither NSTask or NSWorkspace support Info.plist _and_ arguments pre-10.6
	NSString *path = [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"OpenRA.app/Contents/MacOS/OpenRA"];
	NSArray *args = [NSArray arrayWithObjects:[gameURL absoluteString], @"mono", @"--debug", @"OpenRA.Game.exe", @"Game.Mods=ra",nil];

	FSRef appRef;
	CFURLGetFSRef((CFURLRef)[NSURL URLWithString:path], &appRef);

	// Set the launch parameters
	LSApplicationParameters params;
	params.version = 0;
	params.flags = kLSLaunchDefaults;
	params.application = &appRef;
	params.asyncLaunchRefCon = NULL;
	params.environment = NULL; // CFDictionaryRef of environment variables; could be useful
	params.argv = (CFArrayRef)args;
	params.initialEvent = NULL;

	ProcessSerialNumber psn;
	OSStatus err = LSOpenApplication(&params, &psn);

	// Bring the game window to the front
	if (err == noErr)
		SetFrontProcess(&psn);
}

- (NSString *)runUtilityQuery:(NSString *)arg
{
	NSPipe *outPipe = [NSPipe pipe];
    NSMutableArray *taskArgs = [NSMutableArray arrayWithObject:@"OpenRA.Utility.exe"];
	[taskArgs addObject:arg];
	
	NSTask *task = [[NSTask alloc] init];
    [task setCurrentDirectoryPath:[gameURL absoluteString]];
    [task setLaunchPath:@"/Library/Frameworks/Mono.framework/Commands/mono"];
    [task setArguments:taskArgs];
	[task setStandardOutput:outPipe];
	[task setStandardError:[task standardOutput]];
    [task launch];
	NSData *data = [[outPipe fileHandleForReading] readDataToEndOfFile];
	[task waitUntilExit];
    [task release];
	
	return [[[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding] autorelease];
}


- (void)runUtilityQuery:(NSString *)arg handleOutput:(id)obj withMethod:(SEL)sel
{
	NSTask *aTask = [[NSTask alloc] init];
	NSPipe *aPipe = [NSPipe pipe];
	NSFileHandle *readHandle = [aPipe fileHandleForReading];
	
    NSMutableArray *taskArgs = [NSMutableArray arrayWithObject:@"OpenRA.Utility.exe"];
	[taskArgs addObject:arg];
	
    [aTask setCurrentDirectoryPath:[gameURL absoluteString]];
    [aTask setLaunchPath:@"/Library/Frameworks/Mono.framework/Commands/mono"];
    [aTask setArguments:taskArgs];
	[aTask setStandardOutput:aPipe];
    [aTask launch];
	
	NSData *inData = nil;
    while ((inData = [readHandle availableData]) && [inData length])
        [obj performSelector:sel withObject:[NSString stringWithUTF8String:[inData bytes]]];
	[aTask waitUntilExit];
    [aTask release];
}

@end
