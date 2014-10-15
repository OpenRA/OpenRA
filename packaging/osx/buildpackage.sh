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

# Remove unused icon
rm OpenRA.app/Contents/Resources/OpenRA.ico

# Remove broken WinForms applications
rm OpenRA.app/Contents/Resources/OpenRA.Editor.exe
rm OpenRA.app/Contents/Resources/OpenRA.exe

# Set version string
sed "s/{DEV_VERSION}/${1}/" OpenRA.app/Contents/Info.plist.template > OpenRA.app/Contents/Info.plist
rm OpenRA.app/Contents/Info.plist.template

# Add a symlink to libgdiplus.dylib to work around an issue in mono 3.8
ln -s /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib OpenRA.app/Contents/Resources/libgdiplus.dylib

# Package app bundle into a zip and clean up
zip OpenRA-$1 -r -9 OpenRA.app --quiet --symlinks
mv OpenRA-$1.zip $4
rm -rf OpenRA.app
