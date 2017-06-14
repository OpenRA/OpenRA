#!/bin/bash
# OpenRA packaging script for macOS

command -v curl >/dev/null 2>&1 || { echo >&2 "macOS packaging requires curl."; exit 1; }
command -v markdown >/dev/null 2>&1 || { echo >&2 "macOS packaging requires markdown."; exit 1; }

if [[ "$OSTYPE" != "darwin"* ]]; then
	command -v cmake >/dev/null 2>&1 || { echo >&2 "macOS packaging requires cmake."; exit 1; }
	command -v genisoimage >/dev/null 2>&1 || { echo >&2 "macOS packaging requires genisoimage."; exit 1; }
fi

LAUNCHER_TAG="osx-launcher-20170604"

if [ $# -ne "2" ]; then
	echo "Usage: `basename $0` tag outputdir"
    exit 1
fi

# Set the working dir to the location of this script
cd $(dirname $0)

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"

modify_plist() {
    sed "s|$1|$2|g" "$3" > "$3.tmp" && mv "$3.tmp" "$3"
}

# Copies the game files and sets metadata
populate_template() {
	TEMPLATE_DIR="${BUILTDIR}/${1}"
	MOD_ID=${2}
	MOD_NAME=${3}
	cp -r "${BUILTDIR}/OpenRA.app" "${TEMPLATE_DIR}"

	# Copy macOS specific files
	cp "${MOD_ID}.icns" "${TEMPLATE_DIR}/Contents/Resources/OpenRA.icns"
	modify_plist "{MOD_ID}" "${MOD_ID}" "${TEMPLATE_DIR}/Contents/Info.plist"
	modify_plist "{MOD_NAME}" "${MOD_NAME}" "${TEMPLATE_DIR}/Contents/Info.plist"
	modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-${MOD_ID}-${TAG}" "${TEMPLATE_DIR}/Contents/Info.plist"
}

# Deletes from the first argument's mod dirs all the later arguments
delete_mods() {
	pushd "${BUILTDIR}/${1}/Contents/Resources/mods" > /dev/null
	shift
	rm -rf $@
	pushd > /dev/null
}

echo "Building launchers"
curl -s -L -O https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/launcher.zip || exit 3
unzip -qq -d "${BUILTDIR}" launcher.zip
rm launcher.zip
mkdir -p "${BUILTDIR}/.DropDMGBackground"

# Background image is created from source svg in artsrc repository
# exported to tiff at 72 + 144 DPI, then combined using
# tiffutil -cathidpicheck bg.tiff bg2x.tiff -out background.tiff
cp background.tiff "${BUILTDIR}/.DropDMGBackground"

# Finder metadata created using free trial of DropDMG
cp DS_Store "${BUILTDIR}/.DS_Store"

ln -s /Applications/ "${BUILTDIR}/Applications"

modify_plist "{DEV_VERSION}" "${TAG}" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
modify_plist "{FAQ_URL}" "http://wiki.openra.net/FAQ" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
echo "Building core files"

pushd ${SRCDIR} > /dev/null
make osx-dependencies
make core SDK="-sdk:4.5"
make version VERSION="${TAG}"
make install-core gameinstalldir="/Contents/Resources/" DESTDIR="${BUILTDIR}/OpenRA.app"
popd > /dev/null

curl -s -L -O https://raw.githubusercontent.com/wiki/OpenRA/OpenRA/Changelog.md
markdown Changelog.md > "${BUILTDIR}/OpenRA.app/Contents/Resources/CHANGELOG.html"
rm Changelog.md

markdown "${SRCDIR}/README.md" > "${BUILTDIR}/OpenRA.app/Contents/Resources/README.html"
markdown "${SRCDIR}/CONTRIBUTING.md" > "${BUILTDIR}/OpenRA.app/Contents/Resources/CONTRIBUTING.html"

populate_template "OpenRA - Red Alert.app" "ra" "Red Alert"
delete_mods "OpenRA - Red Alert.app" "cnc" "d2k"

populate_template "OpenRA - Tiberian Dawn.app" "cnc" "Tiberian Dawn"
delete_mods "OpenRA - Tiberian Dawn.app" "ra" "d2k"

populate_template "OpenRA - Dune 2000.app" "d2k" "Dune 2000"
delete_mods "OpenRA - Dune 2000.app" "ra" "cnc"

rm -rf "${BUILTDIR}/OpenRA.app"

echo "Packaging disk image"

if [[ "$OSTYPE" == "darwin"* ]]; then
	hdiutil create -volname OpenRA -srcfolder build -ov -format UDZO "${OUTPUTDIR}/OpenRA-${TAG}.dmg"
else
	echo "Building libdmg-hfsplus"

	# Cloning is very slow, so fetch zip instead
	curl -s -L -O https://github.com/OpenRA/libdmg-hfsplus/archive/master.zip || exit 3
	unzip -qq master.zip
	rm master.zip
	pushd libdmg-hfsplus-master > /dev/null
	cmake . > /dev/null
	make > /dev/null
	popd > /dev/null

	if [[ ! -f libdmg-hfsplus-master/dmg/dmg ]] ; then
		echo "libdmg-hfsplus compilation failed"
		exit 3
	fi

	genisoimage -V OpenRA -D -R -apple -no-pad -o build.dmg build
	libdmg-hfsplus-master/dmg/dmg dmg ./build.dmg "${OUTPUTDIR}/OpenRA-${TAG}.dmg"
	rm build.dmg
fi

# Clean up
rm -rf libdmg-hfsplus-master "${BUILTDIR}"
