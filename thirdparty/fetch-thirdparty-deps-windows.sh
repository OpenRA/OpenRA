#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

if [ $# -ne "1" ]; then
	echo "Usage: $(basename "$0") (32|64)"
	exit 1
fi

if [ "$1" != "x86" ] && [ "$1" != "x64" ]; then
	echo "Usage: $(basename "$0") (32|64)"
	exit 1
fi

download_dir="${0%/*}/download/windows"

mkdir -p "${download_dir}"
cd "${download_dir}" || exit 1

if [ ! -f SDL2.dll ]; then
	echo "Fetching SDL2 from libsdl.org"
	if [ "$1" = "x86" ]; then
		curl -LOs https://www.libsdl.org/release/SDL2-2.0.5-win32-x86.zip
		unzip SDL2-2.0.5-win32-x86.zip SDL2.dll
		rm SDL2-2.0.5-win32-x86.zip
	else
		curl -LOs https://www.libsdl.org/release/SDL2-2.0.5-win32-x64.zip
		unzip SDL2-2.0.5-win32-x64.zip SDL2.dll
		rm SDL2-2.0.5-win32-x64.zip
	fi
fi

if [ ! -f freetype6.dll ]; then
	echo "Fetching FreeType2 from NuGet"
	../../noget.sh SharpFont.Dependencies 2.6.0
	if [ "$1" = "x86" ]; then
		cp ./SharpFont.Dependencies/bin/msvc9/x86/freetype6.dll .
	else
		cp ./SharpFont.Dependencies/bin/msvc9/x64/freetype6.dll .
	fi
	rm -rf SharpFont.Dependencies
fi

if [ ! -f lua51.dll ]; then
	echo "Fetching Lua 5.1 from NuGet"
	../../noget.sh lua.binaries 5.1.5
	if [ "$1" = "x86" ]; then
		cp ./lua.binaries/bin/win32/dll8/lua5.1.dll ./lua51.dll
	else
		cp ./lua.binaries/bin/win64/dll8/lua5.1.dll ./lua51.dll
	fi
	rm -rf lua.binaries
fi

if [ ! -f soft_oal.dll ]; then
	echo "Fetching OpenAL Soft from NuGet"
	../../noget.sh OpenAL-Soft 1.16.0
	if [ "$1" = "x86" ]; then
		cp ./OpenAL-Soft/bin/Win32/soft_oal.dll ./soft_oal.dll
	else
		cp ./OpenAL-Soft/bin/Win64/soft_oal.dll ./soft_oal.dll
	fi
	rm -rf OpenAL-Soft
fi
