#!/bin/sh
# OpenRA Packaging script for osx
#    Packages game files into the launcher app
#    previously created by the package-launcher script

PACKAGING_PATH="$1/osxbuild"
SOURCE_PATH="$1"
BUNDLE_PATH="OpenRA.app"
TARGET_PATH="$PACKAGING_PATH/OpenRA.app/Contents/Resources"

FILES="OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll FreeSans.ttf FreeSansBold.ttf titles.ttf shaders mods/ra mods/cnc"
EXCLUDE="*.mdb"

# Copy source files into packaging dir
mkdir -p $PACKAGING_PATH
cp -r "$BUNDLE_PATH" "$PACKAGING_PATH/OpenRA.app"
mkdir -p "$TARGET_PATH/mods"

for i in $FILES; do
	cp -R "$SOURCE_PATH/$i" "$TARGET_PATH/$i"
done

# Delete unwanted files
cd $TARGET_PATH
for i in $EXCLUDE; do
	find . -path "$i" -delete
done

cd $PACKAGING_PATH
zip OpenRA-$2 -r -9 OpenRA.app 
echo "Done!"
