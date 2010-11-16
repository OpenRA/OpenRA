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

-(id)initWithPath:(NSString *)path
{
	self = [super init];
	if (self != nil)
	{
		gamePath = path;
	}
	return self;
}

- (void)dealloc
{
	[utilityBuffer release];
	[super dealloc];
}

-(void)clearBuffer
{
	[utilityBuffer release];
	utilityBuffer = [[NSMutableString stringWithString:@""] retain];
}

- (void)bufferData:(NSString *)string
{
	if (string == nil) return;
	[utilityBuffer appendString:string];
}

- (NSArray *)installedMods
{
	[self clearBuffer];
	[self runUtilityApp:@"-l" handleOutput:self withMethod:@selector(bufferData:)];
	id mods = [[NSString stringWithString:utilityBuffer] stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
	return [mods componentsSeparatedByString:@"\n"];
}

- (NSArray *)infoForMods:(NSArray *)mods
{
	[self clearBuffer];
	[self runUtilityApp:[NSString stringWithFormat:@"-i=%@",[mods componentsJoinedByString:@","]] handleOutput:self withMethod:@selector(bufferData:)];
	NSArray *lines = [utilityBuffer componentsSeparatedByString:@"\n"];
	
	NSMutableArray *ret = [NSMutableArray array];
	NSMutableDictionary *fields = nil;
	NSString *current = nil;
	for (id l in lines)
	{
		id line = [l stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
		if (line == nil || [line length] == 0)
			continue;
		
		id kv = [line componentsSeparatedByString:@":"];
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
				[ret addObject:[Mod modWithId:current fields:fields]];
			NSLog(@"Parsing mod %@",value);
			current = value;
			fields = [NSMutableDictionary dictionary];			
		}
		
		if (fields != nil)
			[fields setObject:[value stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]]
					   forKey:key];
	}
	return ret;
}

-(void)launchGame
{
	// Use LaunchServices because neither NSTask or NSWorkspace support Info.plist _and_ arguments pre-10.6
	NSString *path = [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"OpenRA.app/Contents/MacOS/OpenRA"];
	NSArray *args = [NSArray arrayWithObjects:gamePath, @"mono", @"--debug", @"OpenRA.Game.exe", @"Game.Mods=ra",nil];

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

- (void)runUtilityApp:(NSString *)arg handleOutput:(id)obj withMethod:(SEL)sel
{
	NSTask *aTask = [[NSTask alloc] init];
	NSPipe *aPipe = [NSPipe pipe];
	NSFileHandle *readHandle = [aPipe fileHandleForReading];
	
    NSMutableArray *taskArgs = [NSMutableArray arrayWithObject:@"OpenRA.Utility.exe"];
	[taskArgs addObject:arg];
	
    [aTask setCurrentDirectoryPath:gamePath];
    [aTask setLaunchPath:@"/Library/Frameworks/Mono.framework/Commands/mono"];
    [aTask setArguments:taskArgs];
	[aTask setStandardOutput:aPipe];
    [aTask launch];
	
	NSData *inData = nil;
    while ((inData = [readHandle availableData]) && [inData length])
        [obj performSelector:sel withObject:[NSString stringWithUTF8String:[inData bytes]]];
	
    [aTask release];
}

@end
