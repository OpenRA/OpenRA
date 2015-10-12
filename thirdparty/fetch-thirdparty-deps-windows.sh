#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

download_dir="${0%/*}/download/windows"

mkdir -p "${download_dir}"
cd "${download_dir}"

function get()
{
	if which nuget >/dev/null; then
		nuget install $1 -Version $2 -ExcludeVersion
	else
		../../noget.sh $1 $2
	fi
}

if [ ! -f SDL2.dll ]; then
	echo "Fetching SDL2 from NuGet"
	get sdl2.redist 2.0.3
	cp ./sdl2.redist/build/native/bin/Win32/dynamic/SDL2.dll .
	rm -rf sdl2.redist
fi

if [ ! -f freetype6.dll ]; then
	echo "Fetching FreeType2 from NuGet"
	get SharpFont.Dependencies 2.6.0
	cp ./SharpFont.Dependencies/bin/msvc9/x86/freetype6.dll .
	rm -rf SharpFont.Dependencies
fi

if [ ! -f lua51.dll ]; then
	echo "Fetching Lua 5.1 from NuGet"
	get lua.binaries 5.1.5
	cp ./lua.binaries/bin/win32/dll8/lua5.1.dll ./lua51.dll
	rm -rf lua.binaries
fi

if [ ! -f soft_oal.dll ]; then
	echo "Fetching OpenAL Soft from NuGet"
	get OpenAL-Soft 1.16.0
	cp ./OpenAL-Soft/bin/Win32/soft_oal.dll ./soft_oal.dll
	rm -rf OpenAL-Soft
fi

if [ ! -f ../NsProcess.zip ]; then
	curl -s -L -o ../NsProcess.zip http://nsis.sourceforge.net/mediawiki/images/archive/1/18/20140806212030!NsProcess.zip
fi
