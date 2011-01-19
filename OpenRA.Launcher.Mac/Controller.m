/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "Controller.h"

@implementation Controller

+ (void)initialize
{
	[[NSUserDefaults standardUserDefaults]
	 registerDefaults:[NSDictionary dictionaryWithObject:[[NSBundle mainBundle] resourcePath]
												  forKey:@"gamepath"]];
}

- (void)awakeFromNib
{	
	gamePath = [[NSUserDefaults standardUserDefaults] stringForKey:@"gamepath"];

	hasMono = [self initMono];
	
	NSLog(@"%d, %@",hasMono, monoPath);
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	if (!hasMono)
	{
		NSAlert *alert = [NSAlert alertWithMessageText:@"Mono Framework"
										 defaultButton:@"Download Mono"
									   alternateButton:@"Quit"
										   otherButton:nil
							 informativeTextWithFormat:@"OpenRA requires the Mono Framework version 2.6.7 or later."];
		
		
		if ([alert runModal] == NSAlertDefaultReturn)
			[[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"http://www.go-mono.com/mono-downloads/download.html"]];
		
		[[NSApplication sharedApplication] terminate:self];
	}
	else
	{
		[self launchMod:@"cnc"];
		[NSApp terminate: nil];
	}
}

- (BOOL)initMono
{
	// Find the users mono
	NSPipe *outPipe = [NSPipe pipe];
	NSTask *task = [[NSTask alloc] init];
    [task setLaunchPath:@"/usr/bin/which"];
    [task setArguments:[NSMutableArray arrayWithObject:@"mono"]];
	[task setStandardOutput:outPipe];
	[task setStandardError:[task standardOutput]];
    [task launch];
	
	NSData *data = [[outPipe fileHandleForReading] readDataToEndOfFile];
	[task waitUntilExit];
    [task release];
	
	NSString *temp = [[[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding] autorelease];
	// Remove whitespace and resolve symlinks
	monoPath = [[[temp stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]]
				 stringByResolvingSymlinksInPath] retain];
			
	if (![monoPath length])
		return NO;
	
	// Find the mono version
	outPipe = [NSPipe pipe];
	task = [[NSTask alloc] init];
    [task setLaunchPath:monoPath];
    [task setArguments:[NSMutableArray arrayWithObject:@"--version"]];
	[task setStandardOutput:outPipe];
	[task setStandardError:[task standardOutput]];
    [task launch];
	
	data = [[outPipe fileHandleForReading] readDataToEndOfFile];
	[task waitUntilExit];
    [task release];
	
	NSString *ret = [[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding];
	
	int major = 0;
	int minor = 0;
	int point = 0;
	sscanf([ret UTF8String], "Mono JIT compiler version %d.%d.%d", &major, &minor, &point);
	[ret release];
	NSLog(@"mono %d.%d.%d: %@",major,minor,point,monoPath);
	
	return (major > 2 ||
			(major == 2 && minor > 6) ||
			(major == 2 && minor == 6 && point >= 7));
}

- (void)dealloc
{
	[monoPath release]; monoPath = nil;
	[gamePath release]; gamePath = nil;
	[super dealloc];
}


-(void)launchMod:(NSString *)mod
{
	// Use LaunchServices because neither NSTask or NSWorkspace support Info.plist _and_ arguments pre-10.6
	
	// First argument is the directory to run in
	// Second...Nth arguments are passed to OpenRA.Game.exe
	// Launcher wrapper sets mono --debug, gl renderer and support dir.
	NSArray *args = [NSArray arrayWithObjects:@"--launch", gamePath, monoPath,
					 [NSString stringWithFormat:@"SupportDir=%@",[@"~/Library/Application Support/OpenRA" stringByExpandingTildeInPath]],
					 [NSString stringWithFormat:@"Game.Mods=%@",mod],
					 nil];
	
	FSRef appRef;
	CFURLGetFSRef((CFURLRef)[NSURL URLWithString:[[[NSBundle mainBundle] executablePath] stringByAddingPercentEscapesUsingEncoding:NSASCIIStringEncoding]], &appRef);
	
	// Set the launch parameters
	LSApplicationParameters params;
	params.version = 0;
	params.flags = kLSLaunchDefaults | kLSLaunchNewInstance;
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

#pragma mark Application delegates
- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender
{
	return YES;
}

@end
