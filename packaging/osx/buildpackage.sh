#!/bin/bash
# OpenRA packaging script for Mac OSX

LAUNCHER_TAG="osx-launcher-20150412"

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` tag files-dir outputdir"
    exit 1
fi

# Dirty build dir; last build failed?
if [ -e "OpenRA.app" ]; then
	echo "Error: OpenRA.app already exists"
    exit 2
fi

wget https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/launcher.zip || exit 3
unzip launcher.zip
rm launcher.zip

# Copy the template to build the game package
cp -rv $2/* "OpenRA.app/Contents/Resources/" || exit 3

# Remove unused icon
rm OpenRA.app/Contents/Resources/OpenRA.ico

# Remove broken WinForms applications
rm OpenRA.app/Contents/Resources/OpenRA.exe
rm OpenRA.app/Contents/Resources/OpenRA.Editor.exe

# Remove linux cruft
rm Eluant.dll.config.in

# Set version string
sed "s/{DEV_VERSION}/${1}/" OpenRA.app/Contents/Info.plist > OpenRA.app/Contents/Info.plist.tmp
mv OpenRA.app/Contents/Info.plist.tmp OpenRA.app/Contents/Info.plist

# Package app bundle into a zip and clean up
zip OpenRA-$1 -r -9 OpenRA.app --quiet --symlinks
mv OpenRA-$1.zip $3
rm -rf OpenRA.app
