#!/bin/bash
# OpenRA packaging script for Mac OSX

LAUNCHER_TAG="osx-launcher-20170211"

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` tag files-dir outputdir"
    exit 1
fi

# Dirty build dir; last build failed?
if [ -e "OpenRA.app" ]; then
	echo "Error: OpenRA.app already exists"
    exit 2
fi

curl -s -L -O https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/launcher.zip || exit 3
unzip -qq launcher.zip
rm launcher.zip

# Copy the template to build the game package
cp -r $2/* "OpenRA.app/Contents/Resources/" || exit 3

# Remove unused icon
rm OpenRA.app/Contents/Resources/OpenRA.ico

# Remove WinForms applications
rm OpenRA.app/Contents/Resources/OpenRA.exe

modify_plist() {
    sed "s/$1/$2/" OpenRA.app/Contents/Info.plist > OpenRA.app/Contents/Info.plist.tmp && mv OpenRA.app/Contents/Info.plist.tmp OpenRA.app/Contents/Info.plist
}

# Set version string
modify_plist "{DEV_VERSION}" "${1}"

modify_plist "{DISPLAY_NAME}" OpenRA

# Package app bundle into a zip and clean up
zip OpenRA-$1 -r -9 OpenRA.app --quiet --symlinks
mv OpenRA-$1.zip $3
rm -rf OpenRA.app
