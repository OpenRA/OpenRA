#!/bin/bash

LAUNCHER_TAG="osx-launcher-20171118"

download_dir="${0%/*}/download/osx"
mkdir -p "$download_dir"
cd "$download_dir"

if [ ! -f libSDL2.dylib ]; then
	echo "Fetching OS X SDL2 library from GitHub."
	curl -LOs https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/libSDL2.dylib
fi

if [ ! -f liblua.5.1.dylib ]; then
	echo "Fetching OS X Lua 5.1 library from GitHub."
	curl -LOs https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/liblua.5.1.dylib
fi

if [ ! -f Eluant.dll.config ]; then
	echo "Fetching OS X Lua configuration file from GitHub."
	curl -LOs https://raw.githubusercontent.com/OpenRA/OpenRALauncherOSX/${LAUNCHER_TAG}/dependencies/Eluant.dll.config
fi
