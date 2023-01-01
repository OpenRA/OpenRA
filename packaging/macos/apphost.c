/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

//
// A custom apphost is required (instead of just invoking <arch-dir>/OpenRA directly)
// because macOS will only properly associate dock icons and tooltips to windows that are
// created by a process in the Contents/MacOS directory (not subdirectories).
//
// .NET 6 does not support universal binaries, and the apphost that is created when
// publishing requires the runtime files to exist in the same directory as the launcher.
//

#include <dlfcn.h>
#include <libgen.h>
#include <stdio.h>

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

int main(int argc, char **argv)
{
	void *lib = dlopen(argv[1], RTLD_LAZY);
	if (lib == NULL)
	{
		fprintf(stderr, "Failed to load %s: %s\n", argv[1], dlerror());
		return 1;
	}

	hostfxr_initialize_for_dotnet_command_line_fn hostfxr_initialize_for_dotnet_command_line = (hostfxr_initialize_for_dotnet_command_line_fn)dlsym(lib, "hostfxr_initialize_for_dotnet_command_line");
	if (!hostfxr_initialize_for_dotnet_command_line)
	{
		fprintf(stderr, "Could not load hostfxr_initialize_for_dotnet_command_line(): %s\n", dlerror());
		return 1;
	}

	hostfxr_run_app_fn hostfxr_run_app = (hostfxr_run_app_fn)dlsym(lib, "hostfxr_run_app");
	if (!hostfxr_run_app)
	{
		fprintf(stderr, "Could not load hostfxr_run_app(): %s\n", dlerror());
		return 1;
	}

	hostfxr_close_fn hostfxr_close = (hostfxr_close_fn)dlsym(lib, "hostfxr_close");
	if (!hostfxr_close)
	{
		fprintf(stderr, "Could not load hostfxr_close(): %s\n", dlerror());
		return 1;
	}

	struct hostfxr_initialize_parameters params;
	params.size = sizeof(params);
	params.host_path = argv[0];
	params.dotnet_root = dirname(argv[1]);

	hostfxr_handle host_context_handle;
	hostfxr_initialize_for_dotnet_command_line(
	    argc - 2,
	    &argv[2],
	    &params,
	    &host_context_handle);

	hostfxr_run_app(host_context_handle);

	return hostfxr_close(host_context_handle);
}
