#!/bin/bash
# OpenRA packaging script for macOS
#
# The application bundles will be signed if the following environment variable is defined:
#   MACOS_DEVELOPER_IDENTITY: Certificate name, of the form `Developer\ ID\ Application:\ <name with escaped spaces>`
# If the identity is not already in the default keychain, specify the following environment variables to import it:
#   MACOS_DEVELOPER_CERTIFICATE_BASE64: base64 content of the exported .p12 developer ID certificate.
#                                       Generate using `base64 certificate.p12 | pbcopy`
#   MACOS_DEVELOPER_CERTIFICATE_PASSWORD: password to unlock the MACOS_DEVELOPER_CERTIFICATE_BASE64 certificate
#
# The applicaton bundles will be notarized if the following environment variables are defined:
#   MACOS_DEVELOPER_USERNAME: Email address for the developer account
#   MACOS_DEVELOPER_PASSWORD: App-specific password for the developer account
#

LAUNCHER_TAG="osx-launcher-20200328"

if [ $# -ne "2" ]; then
	echo "Usage: $(basename "$0") tag outputdir"
	exit 1
fi

if [[ "$OSTYPE" != "darwin"* ]]; then
	echo >&2 "macOS packaging requires a macOS host"
	exit 1
fi

# Set the working dir to the location of this script
cd "$(dirname "$0")" || exit 1

# Import code signing certificate
if [ -n "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" ] && [ -n "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	echo "Importing signing certificate"
	echo "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" | base64 --decode > build.p12
	security create-keychain -p build build.keychain
	security default-keychain -s build.keychain
	security unlock-keychain -p build build.keychain
	security import build.p12 -k build.keychain -P "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" -T /usr/bin/codesign >/dev/null 2>&1
	security set-key-partition-list -S apple-tool:,apple: -s -k build build.keychain >/dev/null 2>&1
	rm -fr build.p12
fi

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"

modify_plist() {
	sed "s|$1|$2|g" "$3" > "$3.tmp" && mv "$3.tmp" "$3"
}

# Copies the game files and sets metadata
populate_bundle() {
	TEMPLATE_DIR="${BUILTDIR}/${1}"
	MOD_ID=${2}
	MOD_NAME=${3}
	cp -r "${BUILTDIR}/OpenRA.app" "${TEMPLATE_DIR}"

	# Assemble multi-resolution icon
	iconutil --convert icns ${MOD_ID}.iconset -o "${TEMPLATE_DIR}/Contents/Resources/${MOD_ID}.icns"

	# Copy macOS specific files
	modify_plist "{MOD_ID}" "${MOD_ID}" "${TEMPLATE_DIR}/Contents/Info.plist"
	modify_plist "{MOD_NAME}" "${MOD_NAME}" "${TEMPLATE_DIR}/Contents/Info.plist"
	modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-${MOD_ID}-${TAG}" "${TEMPLATE_DIR}/Contents/Info.plist"
}

# Deletes from the first argument's mod dirs all the later arguments
delete_mods() {
	pushd "${BUILTDIR}/${1}/Contents/Resources/mods" > /dev/null || exit 1
	shift
	rm -rf "$@"
	pushd > /dev/null || exit 1
}

# Sign binaries with developer certificate
sign_bundle() {
	if [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
		codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements entitlements.plist "${BUILTDIR}/${1}/Contents/Resources/"*.dylib
		codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements entitlements.plist --deep "${BUILTDIR}/${1}"
	fi
}

echo "Building launchers"
curl -s -L -O https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${LAUNCHER_TAG}/launcher.zip || exit 3
unzip -qq -d "${BUILTDIR}" launcher.zip
rm launcher.zip

modify_plist "{DEV_VERSION}" "${TAG}" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
modify_plist "{FAQ_URL}" "http://wiki.openra.net/FAQ" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
echo "Building core files"

pushd "${SRCDIR}" > /dev/null || exit 1
make clean
make core TARGETPLATFORM=osx-x64
make version VERSION="${TAG}"
make install-core gameinstalldir="/Contents/Resources/" DESTDIR="${BUILTDIR}/OpenRA.app"
make install-dependencies TARGETPLATFORM=osx-x64 gameinstalldir="/Contents/Resources/"  DESTDIR="${BUILTDIR}/OpenRA.app"
popd > /dev/null || exit 1

populate_bundle "OpenRA - Red Alert.app" "ra" "Red Alert"
delete_mods "OpenRA - Red Alert.app" "cnc" "d2k"
sign_bundle "OpenRA - Red Alert.app"

populate_bundle "OpenRA - Tiberian Dawn.app" "cnc" "Tiberian Dawn"
delete_mods "OpenRA - Tiberian Dawn.app" "ra" "d2k"
sign_bundle "OpenRA - Tiberian Dawn.app"

populate_bundle "OpenRA - Dune 2000.app" "d2k" "Dune 2000"
delete_mods "OpenRA - Dune 2000.app" "ra" "cnc"
sign_bundle "OpenRA - Dune 2000.app"

rm -rf "${BUILTDIR}/OpenRA.app"

if [ -n "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" ] && [ -n "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	security delete-keychain build.keychain
fi

echo "Packaging disk image"
hdiutil create build.dmg -format UDRW -volname "OpenRA" -fs HFS+ -srcfolder build
DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "build.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
sleep 2

# Background image is created from source svg in artsrc repository
mkdir "/Volumes/OpenRA/.background/"
tiffutil -cathidpicheck background.png background-2x.png -out "/Volumes/OpenRA/.background/background.tiff"

cp "${BUILTDIR}/OpenRA - Red Alert.app/Contents/Resources/ra.icns" "/Volumes/OpenRA/.VolumeIcon.icns"

echo '
   tell application "Finder"
     tell disk "'OpenRA'"
           open
           set current view of container window to icon view
           set toolbar visible of container window to false
           set statusbar visible of container window to false
           set the bounds of container window to {400, 100, 1040, 580}
           set theViewOptions to the icon view options of container window
           set arrangement of theViewOptions to not arranged
           set icon size of theViewOptions to 72
           set background picture of theViewOptions to file ".background:background.tiff"
           make new alias file at container window to POSIX file "/Applications" with properties {name:"Applications"}
           set position of item "'OpenRA - Tiberian Dawn.app'" of container window to {160, 106}
           set position of item "'OpenRA - Red Alert.app'" of container window to {320, 106}
           set position of item "'OpenRA - Dune 2000.app'" of container window to {480, 106}
           set position of item "Applications" of container window to {320, 298}
           set position of item ".background" of container window to {160, 298}
           set position of item ".fseventsd" of container window to {160, 298}
           set position of item ".VolumeIcon.icns" of container window to {160, 298}
           update without registering applications
           delay 5
           close
     end tell
   end tell
' | osascript

# HACK: Copy the volume icon again - something in the previous step seems to delete it...?
cp "${BUILTDIR}/OpenRA - Red Alert.app/Contents/Resources/ra.icns" "/Volumes/OpenRA/.VolumeIcon.icns"
SetFile -c icnC "/Volumes/OpenRA/.VolumeIcon.icns"
SetFile -a C "/Volumes/OpenRA"

chmod -Rf go-w /Volumes/OpenRA
sync
sync

hdiutil detach "${DMG_DEVICE}"

# Submit for notarization
if [ -n "${MACOS_DEVELOPER_USERNAME}" ] && [ -n "${MACOS_DEVELOPER_PASSWORD}" ]; then
	echo "Submitting disk image for notarization"

	# Reset xcode search path to fix xcrun not finding altool
	sudo xcode-select -r

	# Create a temporary read-only dmg for submission (notarization service rejects read/write images)
	hdiutil convert build.dmg -format UDZO -imagekey zlib-level=9 -ov -o notarization.dmg

	NOTARIZATION_UUID=$(xcrun altool --notarize-app --primary-bundle-id "net.openra.packaging" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" --file notarization.dmg 2>&1 | awk -F' = ' '/RequestUUID/ { print $2; exit }')
	if [ -z "${NOTARIZATION_UUID}" ]; then
		echo "Submission failed"
		exit 1
	fi

	echo "Submission UUID is ${NOTARIZATION_UUID}"
	rm notarization.dmg

	while :; do
		sleep 30
		NOTARIZATION_RESULT=$(xcrun altool --notarization-info "${NOTARIZATION_UUID}" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" 2>&1 | awk -F': ' '/Status/ { print $2; exit }')
		echo "Submission status: ${NOTARIZATION_RESULT}"

		if [ "${NOTARIZATION_RESULT}" == "invalid" ]; then
			NOTARIZATION_LOG_URL=$(xcrun altool --notarization-info "${NOTARIZATION_UUID}" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" 2>&1 | awk -F': ' '/LogFileURL/ { print $2; exit }')
			echo "Notarization failed with error:"
			curl -s "${NOTARIZATION_LOG_URL}" -w "\n"
			exit 1
		fi

		if [ "${NOTARIZATION_RESULT}" == "success" ]; then
			echo "Stapling notarization tickets"
			DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "build.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
			sleep 2

			xcrun stapler staple "/Volumes/OpenRA/OpenRA - Red Alert.app"
			xcrun stapler staple "/Volumes/OpenRA/OpenRA - Tiberian Dawn.app"
			xcrun stapler staple "/Volumes/OpenRA/OpenRA - Dune 2000.app"

			sync
			sync

			hdiutil detach "${DMG_DEVICE}"
			break
		fi
	done
fi

hdiutil convert build.dmg -format UDZO -imagekey zlib-level=9 -ov -o "${OUTPUTDIR}/OpenRA-${TAG}.dmg"

# Clean up
rm -rf "${BUILTDIR}" build.dmg
