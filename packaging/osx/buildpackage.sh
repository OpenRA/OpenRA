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
	cp -r OpenRA.app "$1"
	cp -r $BUILTDIR/* "$1/Contents/Resources/" || exit 3
	cp "$2.icns" "$1/Contents/Resources/OpenRA.icns"
	modify_plist "{MOD_ID}" "$2" "$1/Contents/Info.plist"
	modify_plist "{MOD_NAME}" "$3" "$1/Contents/Info.plist"
	modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-$2-$TAG" "$1/Contents/Info.plist"
}

# Deletes from the first argument's mod dirs all the later arguments
delete_mods() {
	pushd "$1/Contents/Resources/mods" > /dev/null
	shift
	rm -rf $@
	pushd > /dev/null
}

curl -s -L -O https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/launcher.zip || exit 3
unzip -qq launcher.zip
rm launcher.zip

modify_plist "{DEV_VERSION}" "${1}" "OpenRA.app/Contents/Info.plist"
modify_plist "{FAQ_URL}" "http://wiki.openra.net/FAQ" "OpenRA.app/Contents/Info.plist"

populate_template "OpenRA - Red Alert.app" "ra" "Red Alert"
delete_mods "OpenRA - Red Alert.app" "cnc" "d2k"

populate_template "OpenRA - Tiberian Dawn.app" "cnc" "Tiberian Dawn"
delete_mods "OpenRA - Tiberian Dawn.app" "ra" "d2k"

populate_template "OpenRA - Dune 2000.app" "d2k" "Dune 2000"
delete_mods "OpenRA - Dune 2000.app" "ra" "cnc"

# Package app bundle into a zip and clean up
zip OpenRA-$1 -r -9 "OpenRA - Red Alert.app" "OpenRA - Tiberian Dawn.app" "OpenRA - Dune 2000.app" --quiet --symlinks
mv OpenRA-$1.zip $3
rm -rf OpenRA.app "OpenRA - Red Alert.app" "OpenRA - Tiberian Dawn.app" "OpenRA - Dune 2000.app"
