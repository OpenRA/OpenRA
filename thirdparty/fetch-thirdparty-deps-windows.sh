#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

if [ ! -d windows ]; then
	mkdir windows
fi

if [ ! -f windows/SDL2.dll ]; then
	echo "Fetching SDL2 from nuget"
	nuget install sdl2 -Version 2.0.3
	cp ./sdl2.redist.2.0.3/build/native/bin/Win32/dynamic/SDL2.dll ./windows/
	rm -rf sdl2.2.0.3 sdl2.redist.2.0.3
fi

if [ ! -f windows/freetype6.dll ]; then
	echo "Fetching SharpFont from nuget"
	nuget install SharpFont -Version 2.5.0.1
	cp ./SharpFont.2.5.0.1/Content/freetype6.dll ./windows/
	rm -rf SharpFont.2.5.0.1
fi

if [ ! -f windows/lua51.dll ]; then
	echo "Fetching Lua 5.1 from nuget"
	nuget install lua.binaries -Version 5.1.5
	cp ./lua.binaries.5.1.5/bin/win32/dll8/lua5.1.dll ./windows/lua51.dll
	rm -rf lua.binaries.5.1.5
fi

if [ ! -f windows/zlib1.dll ]; then
	echo "Fetching ZLib from nuget"
	nuget install zlib.redist -Version 1.2.8.7
	cp ./zlib.redist.1.2.8.7/build/native/bin/v120/Win32/Release/dynamic/stdcall/zlib.dll windows/zlib1.dll
	rm -rf zlib.redist.1.2.8.7
fi

if [ ! -f windows/soft_oal.dll ]; then
	echo "Fetching OpenAL Soft from nuget"
	nuget install OpenAL-Soft -Version 1.16.0
	cp ./OpenAL-Soft.1.16.0/bin/Win32/soft_oal.dll windows/soft_oal.dll
	rm -rf OpenAL-Soft.1.16.0
fi
