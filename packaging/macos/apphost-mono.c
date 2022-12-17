/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

//
// A custom apphost is required (instead of just invoking `mono OpenRA.dll ...` directly)
// because macOS will only properly associate dock icons and tooltips to windows that are
// created by a process in the Contents/MacOS directory (not subdirectories).
//
// Based on https://github.com/mono/monodevelop/blob/main/main/build/MacOSX/monostub.mm

#include <dlfcn.h>
#include <stdio.h>
#include <sys/resource.h>

typedef int (* mono_main)(int argc, char **argv);

int main(int argc, char **argv)
{
	// TODO: This snippet increasing the open file limit was copied from
	// the monodevelop launcher stub. It may not be needed for OpenRA.
	struct rlimit limit;
	if (getrlimit(RLIMIT_NOFILE, &limit) == 0 && limit.rlim_cur < 1024)
	{
		limit.rlim_cur = limit.rlim_max < 1024 ? limit.rlim_max : 1024;
		setrlimit(RLIMIT_NOFILE, &limit);
	}

	void *libmono = dlopen(argv[1], RTLD_LAZY);
	if (libmono == NULL)
	{
		fprintf(stderr, "Failed to load libmonosgen-2.0.dylib: %s\n", dlerror());
		return 1;
	}

	mono_main _mono_main = (mono_main)dlsym(libmono, "mono_main");
	if (!_mono_main)
	{
		fprintf(stderr, "Could not load mono_main(): %s\n", dlerror());
		return 1;
	}

	return _mono_main(argc - 1, &argv[1]);
}
