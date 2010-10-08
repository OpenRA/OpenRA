#!/bin/bash
# OpenRA packaging script for Mac OSX

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` version files-dir outputdir"
    exit 1
fi

# Dirty build dir; last build failed?
if [ -e "OpenRA.app" ]; then
	echo "Error: OpenRA.app already exists"
    exit 2
fi

# Copy the template to build the game package
# Assumes it is layed out with the correct directory structure
cp -rv template.app OpenRA.app
cp -rv $2/* "OpenRA.app/Contents/Resources/" || exit 3

# Remove the tao and WindowsBase dlls (they are shipped in the deps package)
rm OpenRA.app/Contents/Resources/Tao.*
rm OpenRA.app/Contents/Resources/WindowsBase.dll

# Icon isn't used, and editor doesn't work under mono 2.8
rm OpenRA.app/Contents/Resources/OpenRA.ico
rm OpenRA.app/Contents/Resources/OpenRA.Editor.exe

# Package app bundle into a zip and clean up
zip OpenRA-$1 -r -9 OpenRA.app
mv OpenRA-$1.zip $3
rm -rf OpenRA.app
