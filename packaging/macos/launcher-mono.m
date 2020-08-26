/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#import <Cocoa/Cocoa.h>
#include <dlfcn.h>

#define SYSTEM_MONO_PATH @"/Library/Frameworks/Mono.framework/Versions/Current/"
#define SYSTEM_MONO_MIN_VERSION @"6.4"

typedef int (* mono_main)(int argc, char **argv);
typedef void (* mono_free)(void *ptr);
typedef char *(* mono_get_runtime_build_info)(void);

@interface OpenRALauncher : NSObject <NSApplicationDelegate>
- (void)launchGameWithArgs: (NSArray *)gameArgs;
@end

@implementation OpenRALauncher

BOOL launched = NO;
NSTask *gameTask;

static int check_mono_version(const char *version, const char *req_version)
{
	char *req_end, *end;
	long req_val, val;

	while (*req_version)
	{
		req_val = strtol(req_version, &req_end, 10);
		if (req_version == req_end || (*req_end && *req_end != '.'))
		{
			fprintf(stderr, "Bad version requirement string '%s'\n", req_end);
			return FALSE;
		}

		req_version = req_end;
		if (*req_version)
			req_version++;

		val = strtol (version, &end, 10);
		if (version == end || val < req_val)
			return FALSE;

		if (val > req_val)
			return TRUE;

		if (*req_version == '.' && *end != '.')
			return FALSE;

		version = end + 1;
	}

	return TRUE;
}

- (int)hasValidMono
{
	void *libmono = dlopen([[SYSTEM_MONO_PATH stringByAppendingPathComponent: @"/lib/libmonosgen-2.0.dylib"] UTF8String], RTLD_LAZY);

	if (libmono == NULL)
	{
		fprintf (stderr, "Failed to load libmonosgen-2.0.dylib: %s\n", dlerror());
		return FALSE;
	}

	mono_main _mono_main = (mono_main)dlsym(libmono, "mono_main");
	if (!_mono_main)
	{
		fprintf(stderr, "Could not load mono_main(): %s\n", dlerror());
		return FALSE;
	}

	mono_free _mono_free = (mono_free)dlsym(libmono, "mono_free");
	if (!_mono_free)
	{
		fprintf(stderr, "Could not load mono_free(): %s\n", dlerror());
		return FALSE;
	}

	mono_get_runtime_build_info _mono_get_runtime_build_info = (mono_get_runtime_build_info)dlsym(libmono, "mono_get_runtime_build_info");
	if (!_mono_get_runtime_build_info)
	{
		fprintf(stderr, "Could not load mono_get_runtime_build_info(): %s\n", dlerror());
		return FALSE;
	}

	char *mono_version = _mono_get_runtime_build_info();
	return check_mono_version(mono_version, [SYSTEM_MONO_MIN_VERSION UTF8String]);
}

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

- (void)launchGameWithArgs: (NSArray *)gameArgs
{
	if (launched)
	{
		NSLog(@"launchgame is already running... ignoring request.");
		return;
	}

	launched = YES;

	if (![self hasValidMono])
		[self exitWithMonoPrompt];

	// Default values - can be overriden by setting certain keys Info.plist
	NSString *gameName = @"OpenRA.dll";
	NSString *modId = nil;

	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist)
	{
		NSString *exeValue = [plist objectForKey:@"MonoGameExe"];
		if (exeValue && [exeValue length] > 0)
			gameName = exeValue;

		NSString *modIdValue = [plist objectForKey:@"ModId"];
		if (modIdValue && [modIdValue length] > 0)
			modId = modIdValue;
	}

	NSString *exePath = [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/MacOS/"];
	NSString *gamePath = [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/Resources/"];

	NSString *launchPath = [SYSTEM_MONO_PATH stringByAppendingPathComponent: @"Commands/mono"];
	NSString *appPath = [exePath stringByAppendingPathComponent: @"OpenRA"];
	NSString *engineLaunchPath = [self resolveTranslocatedPath: appPath];

	NSMutableArray *launchArgs = [NSMutableArray arrayWithCapacity: [gameArgs count] + 2];
	[launchArgs addObject: @"--debug"];
	[launchArgs addObject: [gamePath stringByAppendingPathComponent: gameName]];
	[launchArgs addObject: [NSString stringWithFormat:@"Engine.LaunchPath=\"%@\"", engineLaunchPath]];

	if (modId)
		[launchArgs addObject: [NSString stringWithFormat:@"Game.Mod=%@", modId]];

	[launchArgs addObjectsFromArray: gameArgs];

	NSLog(@"Running launchgame with arguments:");
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
