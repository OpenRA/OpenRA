#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

download_dir="${0%/*}/download/windows"

mkdir -p "${download_dir}"
cd "${download_dir}"

if [ ! -f SDL2.dll ]; then
	echo "Fetching SDL2 from libsdl.org"
	curl -LOs https://www.libsdl.org/release/SDL2-2.0.5-win32-x86.zip
	unzip SDL2-2.0.5-win32-x86.zip SDL2.dll
	rm SDL2-2.0.5-win32-x86.zip
fi

if [ ! -f freetype6.dll ]; then
	echo "Fetching FreeType2 from NuGet"
	../../noget.sh SharpFont.Dependencies 2.6.0
	cp ./SharpFont.Dependencies/bin/msvc9/x86/freetype6.dll .
	rm -rf SharpFont.Dependencies
fi

if [ ! -f lua51.dll ]; then
	echo "Fetching Lua 5.1 from NuGet"
	../../noget.sh lua.binaries 5.1.5
	cp ./lua.binaries/bin/win32/dll8/lua5.1.dll ./lua51.dll
	rm -rf lua.binaries
fi

if [ ! -f soft_oal.dll ]; then
	echo "Fetching OpenAL Soft from NuGet"
	../../noget.sh OpenAL-Soft 1.16.0
	cp ./OpenAL-Soft/bin/Win32/soft_oal.dll ./soft_oal.dll
	rm -rf OpenAL-Soft
fi
