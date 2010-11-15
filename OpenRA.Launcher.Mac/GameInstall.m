//
//  GameInstall.m
//  OpenRA
//
//  Created by Paul Chote on 15/11/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "GameInstall.h"


@implementation GameInstall

-(id)initWithPath:(NSString *)path
{
	self = [super init];
	if (self != nil)
	{
		NSLog(@"creating game at path %@",path);

		gamePath = path;
	}
	return self;
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
@end
