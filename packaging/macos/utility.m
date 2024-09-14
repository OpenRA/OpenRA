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

	int ret = launch_dotnet(argc, argv, modId, isArmArchitecture);

	[pool release];
	return ret;
}
