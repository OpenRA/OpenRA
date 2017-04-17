#!/bin/bash

download_dir="${0%/*}/download/osx"
mkdir -p "$download_dir"
cd "$download_dir"

if [ ! -f libSDL2.dylib ]; then
	echo "Fetching OS X SDL2 library from GitHub."
	curl -LOs https://raw.githubusercontent.com/OpenRA/OpenRALauncherOSX/master/dependencies/libSDL2.dylib
fi

if [ ! -f liblua.5.1.dylib ]; then
	echo "Fetching OS X Lua 5.1 library from GitHub."
	curl -LOs https://raw.githubusercontent.com/OpenRA/OpenRALauncherOSX/master/dependencies/liblua.5.1.dylib
fi

if [ ! -f Eluant.dll.config ]; then
	echo "Fetching OS X Lua configuration file from GitHub."
	curl -LOs https://raw.githubusercontent.com/OpenRA/OpenRALauncherOSX/master/dependencies/Eluant.dll.config
fi
