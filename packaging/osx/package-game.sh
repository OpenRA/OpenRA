#!/bin/sh
# OpenRA Packaging script for osx
#    Packages game files into the launcher app
#    previously created by the package-launcher script

PWD=`pwd`
PACKAGING_PATH="$PWD/osxbuild"
LAUNCHER_PATH="$PACKAGING_PATH/launcher/OpenRA.app"
SOURCE_PATH="$PWD/."

FILES="OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll FreeSans.ttf FreeSansBold.ttf titles.ttf shaders maps mods/ra mods/cnc"
EXCLUDE="*.mdb ./mods/cnc/packages/*.mix ./mods/ra/packages/*.mix  ./mods/cnc/packages/*.MIX ./mods/ra/packages/*.MIX"

# Copy source files into packaging dir
PAYLOAD="$PACKAGING_PATH/payload"
mkdir -p $PAYLOAD
mkdir -p "$PAYLOAD/mods"

for i in $FILES; do
	cp -RX "$i" "$PAYLOAD/$i"
done

# Delete unwanted files
cd $PAYLOAD
for i in $EXCLUDE; do
	find . -path "$i" -delete
done
cd $PACKAGING_PATH

# Move everything into the app bundle
cp -r "$LAUNCHER_PATH" .
cp -r "$PAYLOAD/" "OpenRA.app/Contents/Resources/"
rm -rf $PAYLOAD
zip OpenRA -r -9 OpenRA.app 
echo "Done!"