/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#import <Cocoa/Cocoa.h>
#include <dlfcn.h>
#include <libgen.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/sysctl.h>
#include <sys/resource.h>
#include <mach/machine.h>

#define SYSTEM_MONO_PATH @"/Library/Frameworks/Mono.framework/Versions/Current/"
#define SYSTEM_MONO_MIN_VERSION @"6.4"
#define DOTNET_MIN_MACOS_VERSION 10.15

typedef void* hostfxr_handle;
struct hostfxr_initialize_parameters
{
	size_t size;
	char *host_path;
	char *dotnet_root;
};

typedef int32_t(*hostfxr_initialize_for_dotnet_command_line_fn)(
	int argc,
	char **argv,
	struct hostfxr_initialize_parameters *parameters,
	hostfxr_handle *host_context_handle);

typedef int32_t(*hostfxr_run_app_fn)(const hostfxr_handle host_context_handle);
typedef int32_t(*hostfxr_close_fn)(const hostfxr_handle host_context_handle);
typedef int (*mono_main)(int argc, char **argv);

int launch_mono(int argc, char **argv, char *modId)
{
	NSString *exePath = [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/MacOS/"];
	NSString *hostPath = [SYSTEM_MONO_PATH stringByAppendingPathComponent: @"lib/libmonosgen-2.0.dylib"];
	NSString *dllPath = [exePath stringByAppendingPathComponent: @"mono/OpenRA.Utility.dll"];

	// TODO: This snippet increasing the open file limit was copied from
	// the monodevelop launcher stub. It may not be needed for OpenRA.
	struct rlimit limit;
	if (getrlimit(RLIMIT_NOFILE, &limit) == 0 && limit.rlim_cur < 1024)
	{
		limit.rlim_cur = limit.rlim_max < 1024 ? limit.rlim_max : 1024;
		setrlimit(RLIMIT_NOFILE, &limit);
	}

	void *libmono = dlopen([hostPath UTF8String], RTLD_LAZY);
	if (!libmono)
	{
		NSLog(@"Failed to load libmonosgen-2.0.dylib: %s\n", dlerror());
		return 1;
	}

	mono_main _mono_main = (mono_main)dlsym(libmono, "mono_main");
	if (!_mono_main)
	{
		NSLog(@"Could not load mono_main(): %s\n", dlerror());
		return 1;
	}

	// Insert hostpath, dll and modId as arguments. Overwrite the first argument which was used to launch this application.
	char **newv = malloc((argc + 2) * sizeof(char*));
	if (!newv)
	{
		NSLog(@"Failed to allocate memory for args array.\n");
		return 1;
	}

	newv[0] = (char*)[hostPath UTF8String];
	newv[1] = (char*)[dllPath UTF8String];
	newv[2] = modId;
	for (int i = 1; i < argc; i++)
		newv[i + 2] = argv[i];

	int ret = _mono_main(argc + 2, newv);
	free(newv);

	return ret;
}

int launch_dotnet(int argc, char **argv, char *modId, bool isArmArchitecture)
{
	NSString *exePath = [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/MacOS/"];
	NSString *dllPath;
	NSString *hostPath;

	if (isArmArchitecture)
	{
		hostPath = [exePath stringByAppendingPathComponent: @"arm64/libhostfxr.dylib"];;
		dllPath = [exePath stringByAppendingPathComponent: @"arm64/OpenRA.Utility.dll"];
	}
	else
	{
		hostPath = [exePath stringByAppendingPathComponent: @"x86_64/libhostfxr.dylib"];;
		dllPath = [exePath stringByAppendingPathComponent: @"x86_64/OpenRA.Utility.dll"];
	}

	void *lib = dlopen([hostPath UTF8String], RTLD_LAZY);
	if (!lib)
	{
		NSLog(@"Failed to load %@: %s\n", hostPath, dlerror());
		return 1;
	}

	hostfxr_initialize_for_dotnet_command_line_fn hostfxr_initialize_for_dotnet_command_line = (hostfxr_initialize_for_dotnet_command_line_fn)dlsym(lib, "hostfxr_initialize_for_dotnet_command_line");
	if (!hostfxr_initialize_for_dotnet_command_line)
	{
		NSLog(@"Could not load hostfxr_initialize_for_dotnet_command_line(): %s\n", dlerror());
		return 1;
	}

	hostfxr_run_app_fn hostfxr_run_app = (hostfxr_run_app_fn)dlsym(lib, "hostfxr_run_app");
	if (!hostfxr_run_app)
	{
		NSLog(@"Could not load hostfxr_run_app(): %s\n", dlerror());
		return 1;
	}

	hostfxr_close_fn hostfxr_close = (hostfxr_close_fn)dlsym(lib, "hostfxr_close");
	if (!hostfxr_close)
	{
		NSLog(@"Could not load hostfxr_close(): %s\n", dlerror());
		return 1;
	}

	struct hostfxr_initialize_parameters params;
	params.size = sizeof(params);
	params.host_path = (char*)[[exePath stringByAppendingPathComponent: @"Utility"] UTF8String];
	params.dotnet_root = dirname((char*)[hostPath UTF8String]);

	// Insert dll and modId as arguments. Overwrite the first argument which was used to launch this application.
	char **newv = malloc((argc + 1) * sizeof(char*));
	if (!newv)
	{
		NSLog(@"Failed to allocate memory for args array.\n");
		return 1;
	}

	newv[0] = (char*)[dllPath UTF8String];
	newv[1] = modId;
	for (int i = 1; i < argc; i++)
		newv[i + 1] = argv[i];

	hostfxr_handle host_context_handle;
	hostfxr_initialize_for_dotnet_command_line(
		argc + 1,
		newv,
		&params,
		&host_context_handle);

	hostfxr_run_app(host_context_handle);

	int ret = hostfxr_close(host_context_handle);
	free(newv);

	return ret;
}

int main(int argc, char **argv)
{
	NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];

	size_t size;
	cpu_type_t type;
	size = sizeof(type);
	bool isArmArchitecture = sysctlbyname("hw.cputype", &type, &size, NULL, 0) == 0 && (type & 0xFF) == CPU_TYPE_ARM;

	BOOL useMono = NO;

	// Before 10.15 macOS didn't support arm.
	// Mono is compiled for intel only.
	if (!isArmArchitecture)
	{
		if (@available(macOS 10.15, *))
			useMono = [[[NSProcessInfo processInfo] environment]objectForKey:@"OPENRA_PREFER_MONO"] != nil;
		else
			useMono = YES;
	}

	if (useMono)
	{
		NSTask *task = [[NSTask alloc] init];
		[task setLaunchPath: [[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/MacOS/checkmono"]];
		[task launch];
		[task waitUntilExit];

		if ([task terminationStatus] != 0)
		{
			NSLog(@"Utility requires Mono %@ or later. Please install Mono and try again.\n", SYSTEM_MONO_MIN_VERSION);
			return 1;
		}
	}

	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	char *modId;
	if (plist)
	{
		NSString *modIdValue = [plist objectForKey:@"ModId"];
		if (modIdValue && [modIdValue length] > 0)
			modId = (char*)[modIdValue UTF8String];
	}

	if (!modId)
	{
		NSLog(@"Could not detect ModId\n");
		return 1;
	}

	setenv("ENGINE_DIR", (char*)[[[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent: @"Contents/Resources/"] UTF8String], 1);

	int ret;
	if (useMono)
		ret = launch_mono(argc, argv, modId);
	else
		ret = launch_dotnet(argc, argv, modId, isArmArchitecture);

	[pool release];
	return ret;
}
