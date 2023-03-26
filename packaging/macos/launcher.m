/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#import <Cocoa/Cocoa.h>
#include <dlfcn.h>
#include <sys/types.h>
#include <sys/sysctl.h>
#include <mach/machine.h>

#define SYSTEM_MONO_PATH @"/Library/Frameworks/Mono.framework/Versions/Current/"
#define SYSTEM_MONO_MIN_VERSION @"6.12"
#define DOTNET_MIN_MACOS_VERSION 10.15

@interface OpenRALauncher : NSObject <NSApplicationDelegate>
- (void)launchGameWithArgs: (NSArray *)gameArgs;
@end

@implementation OpenRALauncher

BOOL launched = NO;
NSTask *gameTask;

- (NSString *)modName
{
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist)
	{
		NSString *title = [plist objectForKey:@"CFBundleDisplayName"];
		if (title && [title length] > 0)
			return title;
	}

	return @"OpenRA";
}

- (void)exitWithMonoPrompt
{
	[NSApp setActivationPolicy: NSApplicationActivationPolicyRegular];
	[[NSApplication sharedApplication] activateIgnoringOtherApps:YES];

	NSString *modName = [self modName];
	NSString *title = [NSString stringWithFormat: @"Cannot launch %@", modName];
	NSString *message = [NSString stringWithFormat: @"%@ requires Mono %@ or later. Please install Mono and try again.", modName, SYSTEM_MONO_MIN_VERSION];

	NSAlert *alert = [[NSAlert alloc] init];
	[alert setMessageText:title];
	[alert setInformativeText:message];
	[alert addButtonWithTitle:@"Download Mono"];
	[alert addButtonWithTitle:@"Quit"];
	NSInteger answer = [alert runModal];
	[alert release];

	if (answer == NSAlertFirstButtonReturn)
		[[NSWorkspace sharedWorkspace] openURL: [NSURL URLWithString:@"https://www.mono-project.com/download/"]];

	exit(1);
}

- (void)exitWithCrashPrompt
{
	[NSApp setActivationPolicy: NSApplicationActivationPolicyRegular];
	[[NSApplication sharedApplication] activateIgnoringOtherApps:YES];

	NSString *modName = [self modName];
	NSString *message = [NSString stringWithFormat: @"%@ has encountered a fatal error and must close.\nPlease refer to the crash logs and FAQ for more information.", modName];

	NSAlert *alert = [[NSAlert alloc] init];
	[alert setMessageText:@"Fatal Error"];
	[alert setInformativeText:message];
	[alert addButtonWithTitle:@"View Logs"];
	[alert addButtonWithTitle:@"View FAQ"];
	[alert addButtonWithTitle:@"Quit"];

	NSInteger answer = [alert runModal];
	[alert release];

	if (answer == NSAlertFirstButtonReturn)
	{
		NSString *logDir = [@"~/Library/Application Support/OpenRA/Logs/" stringByExpandingTildeInPath];
		[[NSWorkspace sharedWorkspace] openFile: logDir withApplication:@"Finder"];
	}
	else if (answer == NSAlertSecondButtonReturn)
	{
		NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
		if (plist)
		{
			NSString *faqUrl = [plist objectForKey:@"FaqUrl"];
			if (faqUrl && [faqUrl length] > 0)
				[[NSWorkspace sharedWorkspace] openURL: [NSURL URLWithString:faqUrl]];
		}
	}

	exit(1);
}

// Application was launched via a URL handler
- (void)getUrl:(NSAppleEventDescriptor *)event withReplyEvent:(NSAppleEventDescriptor *)replyEvent
{
	NSMutableArray *gameArgs = [[[NSProcessInfo processInfo] arguments] mutableCopy];
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	NSString *url = [[event paramDescriptorForKeyword:keyDirectObject] stringValue];

	if (plist)
	{
		NSString *joinServerUrl = [plist objectForKey:@"JoinServerUrlScheme"];
		if (joinServerUrl && [joinServerUrl length] > 0)
		{
			NSString *prefix = [joinServerUrl stringByAppendingString: @"://"];
			if ([url hasPrefix: prefix])
			{
				NSString *trimmed = [url substringFromIndex:[prefix length]];
				NSArray *parts = [trimmed componentsSeparatedByString:@":"];
				NSNumberFormatter *formatter = [[NSNumberFormatter alloc] init];

				if ([parts count] == 2 && [formatter numberFromString: [parts objectAtIndex:1]] != nil)
					[gameArgs addObject: [NSString stringWithFormat: @"Launch.Connect=%@", trimmed]];

				[formatter release];
			}
		}
	}

	[self launchGameWithArgs: gameArgs];
    [gameArgs release];
}

- (void)applicationWillFinishLaunching:(NSNotification *)aNotification
{
	// Register for url events
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist)
	{
		NSString *joinServerUrl = [plist objectForKey:@"JoinServerUrlScheme"];
		NSString *bundleIdentifier = [plist objectForKey:@"CFBundleIdentifier"];
		if (joinServerUrl && [joinServerUrl length] > 0 && bundleIdentifier)
		{
			LSSetDefaultHandlerForURLScheme((CFStringRef)joinServerUrl, (CFStringRef)bundleIdentifier);
			[[NSAppleEventManager sharedAppleEventManager] setEventHandler:self andSelector:@selector(getUrl:withReplyEvent:) forEventClass:kInternetEventClass andEventID:kAEGetURL];
		}
	}
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	[self launchGameWithArgs: [[NSProcessInfo processInfo] arguments]];
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed: (NSApplication *)theApplication
{
	return YES;
}

- (int)hasValidMono
{
	NSTask *task = [[NSTask alloc] init];
	[task setLaunchPath: [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/MacOS/checkmono"]];
	[task launch];
	[task waitUntilExit];

	return [task terminationStatus] == 0;
}

- (void)launchGameWithArgs: (NSArray *)gameArgs
{
	if (launched)
	{
		NSLog(@"launchgame is already running... ignoring request.");
		return;
	}

	launched = YES;

	BOOL useMono = NO;

	if (@available(macOS 10.15, *))
		useMono = [[[NSProcessInfo processInfo] environment]objectForKey:@"OPENRA_PREFER_MONO"] != nil;
	else
		useMono = YES;

	if (useMono && ![self hasValidMono])
		[self exitWithMonoPrompt];

	// Default values - can be overriden by setting certain keys Info.plist
	NSString *modId = nil;

	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist)
	{
		NSString *modIdValue = [plist objectForKey:@"ModId"];
		if (modIdValue && [modIdValue length] > 0)
			modId = modIdValue;
	}

	NSString *exePath = [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/MacOS/"];
	NSString *gamePath = [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/Resources/"];

	NSString *launchPath;
	NSString *dllPath;
	NSString *hostPath;

	if (useMono)
	{
		launchPath = [exePath stringByAppendingPathComponent: @"apphost-mono"];
		hostPath = [SYSTEM_MONO_PATH stringByAppendingPathComponent: @"lib/libmonosgen-2.0.dylib"];;
		dllPath = [exePath stringByAppendingPathComponent: @"mono/OpenRA.dll"];
	}
	else
	{
		size_t size;
		cpu_type_t type;
		size = sizeof(type);

		if (sysctlbyname("hw.cputype", &type, &size, NULL, 0) == 0 && (type & 0xFF) == CPU_TYPE_ARM)
		{
			launchPath = [exePath stringByAppendingPathComponent: @"apphost-arm64"];
			hostPath = [exePath stringByAppendingPathComponent: @"arm64/libhostfxr.dylib"];;
			dllPath = [exePath stringByAppendingPathComponent: @"arm64/OpenRA.dll"];
		}
		else
		{
			launchPath = [exePath stringByAppendingPathComponent: @"apphost-x86_64"];
			hostPath = [exePath stringByAppendingPathComponent: @"x86_64/libhostfxr.dylib"];;
			dllPath = [exePath stringByAppendingPathComponent: @"x86_64/OpenRA.dll"];
		}
	}

	NSString *appPath = [exePath stringByAppendingPathComponent: @"Launcher"];
	NSString *engineLaunchPath = [self resolveTranslocatedPath: appPath];

	NSMutableArray *launchArgs = [NSMutableArray arrayWithCapacity: [gameArgs count] + 5];
	[launchArgs addObject: hostPath];
	[launchArgs addObject: dllPath];
	[launchArgs addObject: [NSString stringWithFormat:@"Engine.LaunchPath=\"%@\"", engineLaunchPath]];
	[launchArgs addObject: [NSString stringWithFormat:@"Engine.EngineDir=../../Resources"]];

	if (modId)
		[launchArgs addObject: [NSString stringWithFormat:@"Game.Mod=%@", modId]];

	[launchArgs addObjectsFromArray: gameArgs];

	NSLog(@"Running OpenRA with arguments:");
	for (size_t i = 0; i < [launchArgs count]; i++)
		NSLog(@"%@", [launchArgs objectAtIndex: i]);

	gameTask = [[NSTask alloc] init];
	[gameTask setCurrentDirectoryPath: gamePath];
	[gameTask setLaunchPath: launchPath];
	[gameTask setArguments: launchArgs];

	[[NSNotificationCenter defaultCenter]
		addObserver: self
		selector: @selector(taskExited:)
		name: NSTaskDidTerminateNotification
		object: gameTask
	];

	[gameTask launch];
}

- (NSString *)resolveTranslocatedPath: (NSString *)path
{
	// macOS 10.12 introduced the "App Translocation" feature, which runs quarantined applications
	// from a transient read-only disk image.  The read-only image isn't a problem, but the transient
	// path breaks the mod registration/switching feature.
	// This resolves the original path which can then be written into the mod metadata for future
	// launches (which will then be re-translocated)

	// Running on macOS < 10.12
	if (floor(NSAppKitVersionNumber) <= 1404)
		return path;

	void *handle = dlopen("/System/Library/Frameworks/Security.framework/Security", RTLD_LAZY);

	// Failed to load security framework
	if (handle == NULL)
		return path;

	Boolean (*mySecTranslocateIsTranslocatedURL)(CFURLRef path, bool *isTranslocated, CFErrorRef * __nullable error);
	mySecTranslocateIsTranslocatedURL = dlsym(handle, "SecTranslocateIsTranslocatedURL");

	CFURLRef __nullable (*mySecTranslocateCreateOriginalPathForURL)(CFURLRef translocatedPath, CFErrorRef * __nullable error);
	mySecTranslocateCreateOriginalPathForURL = dlsym(handle, "SecTranslocateCreateOriginalPathForURL");

	// Failed to resolve required functions
	if (mySecTranslocateIsTranslocatedURL == NULL || mySecTranslocateCreateOriginalPathForURL == NULL)
		return path;

	bool isTranslocated = false;
	CFURLRef pathURLRef = CFURLCreateWithFileSystemPath(kCFAllocatorDefault, (__bridge CFStringRef)path, kCFURLPOSIXPathStyle, false);

	if (mySecTranslocateIsTranslocatedURL(pathURLRef, &isTranslocated, NULL))
	{
		if (isTranslocated)
		{
			CFURLRef resolvedURL = mySecTranslocateCreateOriginalPathForURL(pathURLRef, NULL);
			path = [(NSURL *)(resolvedURL) path];
		}
	}

	CFRelease(pathURLRef);
	return path;
}

- (void)taskExited:(NSNotification *)note
{
	[[NSNotificationCenter defaultCenter]
		removeObserver:self
		name:NSTaskDidTerminateNotification
		object:gameTask
	];

	int ret = [gameTask terminationStatus];
	NSLog(@"launchgame exited with code %d", ret);
	[gameTask release];
	gameTask = nil;

	// We're done here
	if (ret != 0)
		[self exitWithCrashPrompt];

	exit(0);
}

@end

int main(int argc, char **argv)
{
	NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];
	NSApplication *application = [NSApplication sharedApplication];
	OpenRALauncher *launcher = [[OpenRALauncher alloc] init];
	[NSApp setActivationPolicy: NSApplicationActivationPolicyProhibited];

	[application setDelegate:launcher];
	[application run];

	[launcher release];
	[pool drain];

	return EXIT_SUCCESS;
}
