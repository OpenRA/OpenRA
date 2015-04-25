#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

if [ ! -d windows ]; then
	mkdir windows
fi

if [ ! -f windows/SDL2.dll ]; then
	echo "Fetching SDL2 from nuget"
	nuget install sdl2 -Version 2.0.3 -ExcludeVersion
	cp ./sdl2.redist/build/native/bin/Win32/dynamic/SDL2.dll ./windows/
	rm -rf sdl2.2.0.3 sdl2.redist
fi

if [ ! -f windows/freetype6.dll ]; then
	echo "Fetching FreeType2 from nuget"
	nuget install freetype2.redist -Version 2.4.11.3 -ExcludeVersion
	cp ./freetype2.redist/bin/win32/zlib1.dll ./windows/
	cp ./freetype2.redist/bin/win32/freetype6.dll ./windows/
	rm -rf freetype2.redist
fi

if [ ! -f windows/lua51.dll ]; then
	echo "Fetching Lua 5.1 from nuget"
	nuget install lua.binaries -Version 5.1.5 -ExcludeVersion
	cp ./lua.binaries/bin/win32/dll8/lua5.1.dll ./windows/lua51.dll
	rm -rf lua.binaries
fi

if [ ! -f windows/zlib1.dll ]; then
	echo "Fetching ZLib from nuget"
	nuget install freetype2.redist -Version 2.4.11.3 -ExcludeVersion
	cp ./freetype2.redist/bin/win32/zlib1.dll ./windows/
	rm -rf freetype2.redist
fi

if [ ! -f windows/soft_oal.dll ]; then
	echo "Fetching OpenAL Soft from nuget"
	nuget install OpenAL-Soft -Version 1.16.0 -ExcludeVersion
	cp ./OpenAL-Soft/bin/Win32/soft_oal.dll windows/soft_oal.dll
	rm -rf OpenAL-Soft
fi
