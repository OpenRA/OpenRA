#!/bin/bash
# OpenRA packaging script for Mac OSX

LAUNCHER_TAG="osx-launcher-20170414"

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` tag files-dir outputdir"
    exit 1
fi

# Dirty build dir; last build failed?
if [ -e "OpenRA.app" ]; then
	echo "Error: OpenRA.app already exists"
    exit 2
fi

TAG="$1"
BUILTDIR="$2"
OUTPUTDIR="$3"

modify_plist() {
    sed "s|$1|$2|g" "$3" > "$3.tmp" && mv "$3.tmp" "$3"
}

# Copies the game files and sets metadata
populate_template() {
	cp -r OpenRA.app "build/$1"
	cp -r $BUILTDIR/* "build/$1/Contents/Resources/" || exit 3
	cp "$2.icns" "build/$1/Contents/Resources/OpenRA.icns"
	modify_plist "{MOD_ID}" "$2" "build/$1/Contents/Info.plist"
	modify_plist "{MOD_NAME}" "$3" "build/$1/Contents/Info.plist"
	modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-$2-$TAG" "build/$1/Contents/Info.plist"
}

# Deletes from the first argument's mod dirs all the later arguments
delete_mods() {
	pushd "build/$1/Contents/Resources/mods" > /dev/null
	shift
	rm -rf $@
	pushd > /dev/null
}

echo "Building libdmg-hfsplus"
# Cloning is very slow, so fetch zip instead
curl -s -L -O https://github.com/OpenRA/libdmg-hfsplus/archive/master.zip || exit 3
unzip -qq master.zip
rm master.zip
pushd libdmg-hfsplus-master > /dev/null
cmake . > /dev/null
make > /dev/null
popd > /dev/null

echo "Building launchers"
curl -s -L -O https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/launcher.zip || exit 3
unzip -qq launcher.zip
rm launcher.zip
mkdir -p build/.DropDMGBackground

# Background image is created from source svg in artsrc repository
# exported to tiff at 72 + 144 DPI, then combined using
# tiffutil -cathidpicheck bg.tiff bg2x.tiff -out background.tiff
cp background.tiff build/.DropDMGBackground

# Finder metadata created using free trial of DropDMG
cp DS_Store build/.DS_Store

ln -s /Applications/ build/Applications

modify_plist "{DEV_VERSION}" "${1}" "OpenRA.app/Contents/Info.plist"
modify_plist "{FAQ_URL}" "http://wiki.openra.net/FAQ" "OpenRA.app/Contents/Info.plist"

populate_template "OpenRA - Red Alert.app" "ra" "Red Alert"
delete_mods "OpenRA - Red Alert.app" "cnc" "d2k"

populate_template "OpenRA - Tiberian Dawn.app" "cnc" "Tiberian Dawn"
delete_mods "OpenRA - Tiberian Dawn.app" "ra" "d2k"

populate_template "OpenRA - Dune 2000.app" "d2k" "Dune 2000"
delete_mods "OpenRA - Dune 2000.app" "ra" "cnc"

echo "Packaging disk image"
genisoimage -V OpenRA -D -R -apple -no-pad -o build.dmg build
libdmg-hfsplus-master/dmg/dmg dmg ./build.dmg "$3/OpenRA-$1.dmg"

# Clean up
rm -rf OpenRA.app build.dmg libdmg-hfsplus-master build
