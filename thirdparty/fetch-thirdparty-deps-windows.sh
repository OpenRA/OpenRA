#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

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