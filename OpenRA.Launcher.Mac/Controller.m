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

extern char **environ;
- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	NSArray *args = [[NSProcessInfo processInfo] arguments];
	gamePath = [[NSUserDefaults standardUserDefaults] stringForKey:@"gamepath"];
	
	// Try and launch the game
	if (![self initMono])
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
	
	// Ingame requests for native dialogs
	if ([args containsObject:@"--display-filepicker"])
		[self launchFilePicker:args];
	
	// Extract a zip file
	else if ([args containsObject:@"--extract-zip"])
		[self extractZip:args];
	
	// Install ra packages from cd
	else if ([args containsObject:@"--install-ra-packages"])
		[self installRAPackages:args];
	
	else 
		[self launch];
	
	[NSApp terminate: nil];
}

- (void)extractZip:(NSArray *)args
{
	// Todo: check if we can write to the requested dir, escalate priviledges if required.
	NSArray *a = [NSArray arrayWithObjects:@"--extract-zip-inner", [args objectAtIndex:2], [args objectAtIndex:3], nil];
	[self runUtilityWithArgs:a];
}

- (void)installRAPackages:(NSArray *)args
{
	// Todo: check if we can write to the requested dir, escalate priviledges if required.
	NSArray *a = [NSArray arrayWithObjects:@"--install-ra-packages-inner", [args objectAtIndex:2], [args objectAtIndex:3], nil];
	[self runUtilityWithArgs:a];
}

- (void)launchFilePicker:(NSArray *)args
{
	[NSApp activateIgnoringOtherApps:YES];
	
	if ([self shouldHideMenubar])
		[NSMenu setMenuBarVisible:NO];
	
	NSOpenPanel *op = [NSOpenPanel openPanel];
	[op setLevel:CGShieldingWindowLevel()];
	[op setAllowsMultipleSelection:NO];
	
	NSUInteger a = [args indexOfObject:@"--display-filepicker"];
	if (a != NSNotFound)
		[op setTitle:[args objectAtIndex:a+1]];
		
	if ([op runModal] == NSFileHandlingPanelOKButton)
		printf("%s\n", [[[op URL] path] UTF8String]);
	
	[NSApp terminate: nil];	
}

- (BOOL)shouldHideMenubar
{
	NSTask *task = [[[NSTask alloc] init] autorelease];
	NSPipe *pipe = [NSPipe pipe];
	
    NSMutableArray *taskArgs = [NSMutableArray arrayWithObjects:@"OpenRA.Utility.exe",
								@"--settings-value",
								[@"~/Library/Application Support/OpenRA" stringByExpandingTildeInPath],
								@"Graphics.Mode", nil];
	
    [task setCurrentDirectoryPath:gamePath];
    [task setLaunchPath:monoPath];
    [task setArguments:taskArgs];
	[task setStandardOutput:pipe];
	NSFileHandle *readHandle = [pipe fileHandleForReading];
	[task launch];
	[task waitUntilExit];
	NSString *response = [[[NSString alloc] initWithData:[readHandle readDataToEndOfFile]
										   encoding: NSUTF8StringEncoding] autorelease];
	return ![response isEqualToString:@"Windowed\n"];
}

-(void)launch
{
	// Use LaunchServices because neither NSTask or NSWorkspace support Info.plist _and_ arguments pre-10.6
	
	// First argument is the directory to run in
	// Second...Nth arguments are passed to OpenRA.Game.exe
	// Launcher wrapper sets mono --debug, gl renderer and support dir.
	NSArray *args = [NSArray arrayWithObjects:@"--launch",
					 [self shouldHideMenubar] ? @"--hide-menubar" : @"--no-hide-menubar",
					 gamePath,
					 monoPath,
					 [NSString stringWithFormat:@"UtilityPath=%@", [[[NSBundle mainBundle] executablePath] stringByAddingPercentEscapesUsingEncoding:NSASCIIStringEncoding]],
					 [NSString stringWithFormat:@"SupportDir=%@",[@"~/Library/Application Support/OpenRA" stringByExpandingTildeInPath]],
					 [NSString stringWithFormat:@"SpecialPackageRoot=%@/",[@"~/Library/Application Support/OpenRA" stringByExpandingTildeInPath]],
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
	
	return (major > 2 ||
			(major == 2 && minor > 6) ||
			(major == 2 && minor == 6 && point >= 7));
}


- (void)runUtilityWithArgs:(NSArray *)args
{
	NSTask *task = [[[NSTask alloc] init] autorelease];
	NSPipe *pipe = [NSPipe pipe];
	
    NSMutableArray *taskArgs = [NSMutableArray arrayWithObject:@"OpenRA.Utility.exe"];
	[taskArgs addObjectsFromArray:args];
		
    [task setCurrentDirectoryPath:gamePath];
    [task setLaunchPath:monoPath];
    [task setArguments:taskArgs];
	[task setStandardOutput:pipe];
	
	NSFileHandle *readHandle = [pipe fileHandleForReading];
	NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
	[nc addObserver:self
		   selector:@selector(utilityResponded:)
			   name:NSFileHandleReadCompletionNotification
			 object:readHandle];
    [task launch];
	[readHandle readInBackgroundAndNotify];
	[task waitUntilExit];
	
	[nc removeObserver:self name:NSFileHandleReadCompletionNotification object:[[task standardOutput] fileHandleForReading]];
	[nc removeObserver:self name:NSTaskDidTerminateNotification object:task];
}

- (void)utilityResponded:(NSNotification *)n
{
	NSData *data = [[n userInfo] valueForKey:NSFileHandleNotificationDataItem];
	NSString *response = [[[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding] autorelease];
	printf("%s", [response UTF8String]);
	
	// Keep reading
	if ([n object] != nil)
		[[n object] readInBackgroundAndNotify];
}


- (void)dealloc
{
	[monoPath release]; monoPath = nil;
	[gamePath release]; gamePath = nil;
	[super dealloc];
}

@end
