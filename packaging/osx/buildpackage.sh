#!/bin/bash
# OpenRA packaging script for Mac OSX

if [ $# -ne "4" ]; then
	echo "Usage: `basename $0` tag files-dir platform-files-dir outputdir"
    exit 1
fi

# Dirty build dir; last build failed?
if [ -e "OpenRA.app" ]; then
	echo "Error: OpenRA.app already exists"
    exit 2
fi

# Copy the template to build the game package
cp -rv template.app OpenRA.app
cp -rv $2/* $3/* "OpenRA.app/Contents/Resources/" || exit 3

# Icon isn't used, and editor doesn't work.
rm OpenRA.app/Contents/Resources/OpenRA.ico
rm OpenRA.app/Contents/Resources/OpenRA.Editor.exe

# SDL2 is the only supported renderer
rm -rf OpenRA.app/Contents/Resources/cg
rm OpenRA.app/Contents/Resources/OpenRA.Renderer.Cg.dll
rm OpenRA.app/Contents/Resources/Tao.Sdl.*
rm OpenRA.app/Contents/Resources/Tao.Cg.*

# Change the .config to use the packaged SDL
sed "s/\/Library\/Frameworks\/SDL2.framework/./" OpenRA.app/Contents/Resources/SDL2-CS.dll.config > temp
mv temp OpenRA.app/Contents/Resources/SDL2-CS.dll.config
rm temp

# Package app bundle into a zip and clean up
zip OpenRA-$1 -r -9 OpenRA.app
mv OpenRA-$1.zip $4
rm -rf OpenRA.app
