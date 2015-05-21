#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

download_dir="${0%/*}/download/windows"

mkdir -p "${download_dir}"
cd "${download_dir}"

if [ ! -f SDL2.dll ]; then
	echo "Fetching SDL2 from nuget"
	nuget install sdl2 -Version 2.0.3 -ExcludeVersion
	cp ./sdl2.redist/build/native/bin/Win32/dynamic/SDL2.dll .
	rm -rf sdl2 sdl2.redist
fi

if [ ! -f freetype6.dll ]; then
	echo "Fetching FreeType2 from nuget"
	nuget install SharpFont.Dependencies -Version 2.5.5.1 -ExcludeVersion
	cp ./SharpFont.Dependencies/bin/msvc10/x86/freetype6.dll .
	rm -rf SharpFont.Dependencies
fi

if [ ! -f lua51.dll ]; then
	echo "Fetching Lua 5.1 from nuget"
	nuget install lua.binaries -Version 5.1.5 -ExcludeVersion
	cp ./lua.binaries/bin/win32/dll8/lua5.1.dll ./lua51.dll
	rm -rf lua.binaries
fi

if [ ! -f soft_oal.dll ]; then
	echo "Fetching OpenAL Soft from nuget"
	nuget install OpenAL-Soft -Version 1.16.0 -ExcludeVersion
	cp ./OpenAL-Soft/bin/Win32/soft_oal.dll ./soft_oal.dll
	rm -rf OpenAL-Soft
fi

if [ ! -f ../NsProcess.zip ]; then
	curl -s -L -o ../NsProcess.zip http://nsis.sourceforge.net/mediawiki/images/archive/1/18/20140806212030!NsProcess.zip
fi
