/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

//
// NOTE: Mono.framework only ships intel dylibs, so cannot be loaded by the arm64 slice of the Launcher utility.
// Splitting checkmono into its own intel-only utility allows it to be called through rosetta if the user
// wants to force the game to run under mono-through-rosetta.
//
// Based on https://github.com/mono/monodevelop/blob/main/main/build/MacOSX/monostub.mm and https://github.com/mono/monodevelop/blob/main/main/build/MacOSX/monostub-utils.h

#include <stdio.h>
#include <stdlib.h>
#include <dlfcn.h>

#define SYSTEM_MONO_PATH "/Library/Frameworks/Mono.framework/Versions/Current/"
#define SYSTEM_MONO_MIN_VERSION "6.12"

typedef char *(* mono_get_runtime_build_info)(void);

int main(int argc, char **argv)
{
	void *libmono = dlopen(SYSTEM_MONO_PATH "lib/libmonosgen-2.0.dylib", RTLD_LAZY);
	if (libmono == NULL)
	{
		fprintf (stderr, "Failed to load libmonosgen-2.0.dylib: %s\n", dlerror());
		return 1;
	}

	mono_get_runtime_build_info _mono_get_runtime_build_info = (mono_get_runtime_build_info)dlsym(libmono, "mono_get_runtime_build_info");
	if (!_mono_get_runtime_build_info)
	{
		fprintf(stderr, "Could not load mono_get_runtime_build_info(): %s\n", dlerror());
		return 1;
	}

	char *version = _mono_get_runtime_build_info();
	char *req_end, *end;
	long req_val, val;
	char *req_version = SYSTEM_MONO_MIN_VERSION;

	while (*req_version && *version)
	{
		req_val = strtol(req_version, &req_end, 10);
		if (req_version == req_end || (*req_end && *req_end != '.'))
		{
			fprintf(stderr, "Bad version requirement string '%s'\n", req_end);
			return 1;
		}

		val = strtol(version, &end, 10);
		if (version == end || val < req_val)
			return 1;

		if (val > req_val)
			return 0;

		if (*req_end == '.' && *end != '.')
			return 1;

		req_version = req_end;
		if (*req_version)
			req_version++;

		version = end + 1;
	}

	return 0;
}
