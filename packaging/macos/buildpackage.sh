#!/bin/bash
# OpenRA packaging script for macOS
#
# The application bundles will be signed if the following environment variables are defined:
#   MACOS_DEVELOPER_IDENTITY: The alphanumeric identifier listed in the certificate name ("Developer ID Application: <your name> (<identity>)")
#                             or as Team ID in your Apple Developer account Membership Details.
# If the identity is not already in the default keychain, specify the following environment variables to import it:
#   MACOS_DEVELOPER_CERTIFICATE_BASE64: base64 content of the exported .p12 developer ID certificate.
#                                       Generate using `base64 certificate.p12 | pbcopy`
#   MACOS_DEVELOPER_CERTIFICATE_PASSWORD: password to unlock the MACOS_DEVELOPER_CERTIFICATE_BASE64 certificate
#
# The applicaton bundles will be notarized if the following environment variables are defined:
#   MACOS_DEVELOPER_USERNAME: Email address for the developer account
#   MACOS_DEVELOPER_PASSWORD: App-specific password for the developer account
#
set -e

MONO_TAG="osx-launcher-20201222"

if [ $# -ne "2" ]; then
	echo "Usage: $(basename "$0") tag outputdir"
	exit 1
fi

if [[ "${OSTYPE}" != "darwin"* ]]; then
	echo >&2 "macOS packaging requires a macOS host"
	exit 1
fi

# Set the working dir to the location of this script
cd "$(dirname "${0}")" || exit 1
. ../functions.sh

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

TAG="${1}"
OUTPUTDIR="${2}"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"
ARTWORK_DIR="$(pwd)/../artwork/"

modify_plist() {
	sed "s|${1}|${2}|g" "${3}" > "${3}.tmp" && mv "${3}.tmp" "${3}"
}

# Copies the game files and sets metadata
build_app() {
	TEMPLATE_DIR="${1}"
	LAUNCHER_DIR="${2}"
	MOD_ID="${3}"
	MOD_NAME="${4}"
	DISCORD_APPID="${5}"

	LAUNCHER_CONTENTS_DIR="${LAUNCHER_DIR}/Contents"
	LAUNCHER_RESOURCES_DIR="${LAUNCHER_CONTENTS_DIR}/Resources"

	cp -r "${TEMPLATE_DIR}" "${LAUNCHER_DIR}"

	IS_D2K="False"
	if [ "${MOD_ID}" = "d2k" ]; then
		IS_D2K="True"
	fi

	# Install engine and mod files
	install_assemblies_mono "${SRCDIR}" "${LAUNCHER_RESOURCES_DIR}" "osx-x64" "True" "True" "${IS_D2K}"
	install_data "${SRCDIR}" "${LAUNCHER_RESOURCES_DIR}" "${MOD_ID}"
	set_engine_version "${TAG}" "${LAUNCHER_RESOURCES_DIR}"
	set_mod_version "${TAG}" "${LAUNCHER_RESOURCES_DIR}/mods/${MOD_ID}/mod.yaml" "${LAUNCHER_RESOURCES_DIR}/mods/modcontent/mod.yaml"

	# Assemble multi-resolution icon
	mkdir "${MOD_ID}.iconset"
	cp "${ARTWORK_DIR}/${MOD_ID}_16x16.png" "${MOD_ID}.iconset/icon_16x16.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_32x32.png" "${MOD_ID}.iconset/icon_16x16@2.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_32x32.png" "${MOD_ID}.iconset/icon_32x32.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_64x64.png" "${MOD_ID}.iconset/icon_32x32@2x.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_128x128.png" "${MOD_ID}.iconset/icon_128x128.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_256x256.png" "${MOD_ID}.iconset/icon_128x128@2x.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_256x256.png" "${MOD_ID}.iconset/icon_256x256.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_512x512.png" "${MOD_ID}.iconset/icon_256x256@2x.png"
	cp "${ARTWORK_DIR}/${MOD_ID}_1024x1024.png" "${MOD_ID}.iconset/icon_512x512@2x.png"
	iconutil --convert icns "${MOD_ID}.iconset" -o "${LAUNCHER_RESOURCES_DIR}/${MOD_ID}.icns"
	rm -rf "${MOD_ID}.iconset"

	# Set launcher metadata
	modify_plist "{MOD_ID}" "${MOD_ID}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
	modify_plist "{MOD_NAME}" "${MOD_NAME}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
	modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-${MOD_ID}-${TAG}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
	modify_plist "{DISCORD_URL_SCHEME}" "discord-${DISCORD_APPID}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"

	# Sign binaries with developer certificate
	if [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
		codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements entitlements.plist "${LAUNCHER_RESOURCES_DIR}/"*.dylib
		codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements entitlements.plist --deep "${LAUNCHER_DIR}"
	fi
}

build_platform() {
	PLATFORM="${1}"
	DMG_PATH="${2}"
	echo "Building launchers (${PLATFORM})"

	# Prepare generic template for the mods to duplicate and customize
	TEMPLATE_DIR="${BUILTDIR}/template.app"
	mkdir -p "${TEMPLATE_DIR}/Contents/Resources"
	mkdir -p "${TEMPLATE_DIR}/Contents/MacOS"
	echo "APPL????" > "${TEMPLATE_DIR}/Contents/PkgInfo"
	cp Info.plist.in "${TEMPLATE_DIR}/Contents/Info.plist"
	modify_plist "{DEV_VERSION}" "${TAG}" "${TEMPLATE_DIR}/Contents/Info.plist"
	modify_plist "{FAQ_URL}" "http://wiki.openra.net/FAQ" "${TEMPLATE_DIR}/Contents/Info.plist"

	if [ "${PLATFORM}" = "compat" ]; then
		modify_plist "{MINIMUM_SYSTEM_VERSION}" "10.9" "${TEMPLATE_DIR}/Contents/Info.plist"
		clang -m64 launcher-mono.m -o "${TEMPLATE_DIR}/Contents/MacOS/OpenRA" -framework AppKit -mmacosx-version-min=10.9
	else
		modify_plist "{MINIMUM_SYSTEM_VERSION}" "10.13" "${TEMPLATE_DIR}/Contents/Info.plist"
		clang -m64 launcher.m -o "${TEMPLATE_DIR}/Contents/MacOS/OpenRA" -framework AppKit -mmacosx-version-min=10.13

		curl -s -L -O https://github.com/OpenRA/OpenRALauncherOSX/releases/download/${MONO_TAG}/mono.zip || exit 3
		unzip -qq -d "${BUILTDIR}/mono" mono.zip
		mv "${BUILTDIR}/mono/mono" "${TEMPLATE_DIR}/Contents/MacOS/"
		mv "${BUILTDIR}/mono/etc" "${TEMPLATE_DIR}/Contents/Resources"
		mv "${BUILTDIR}/mono/lib" "${TEMPLATE_DIR}/Contents/Resources"
		rm mono.zip
		rmdir "${BUILTDIR}/mono"
	fi

	build_app "${TEMPLATE_DIR}" "${BUILTDIR}/OpenRA - Red Alert.app" "ra" "Red Alert" "699222659766026240"
	build_app "${TEMPLATE_DIR}" "${BUILTDIR}/OpenRA - Tiberian Dawn.app" "cnc" "Tiberian Dawn" "699223250181292033"
	build_app "${TEMPLATE_DIR}" "${BUILTDIR}/OpenRA - Dune 2000.app" "d2k" "Dune 2000" "712711732770111550"

	rm -rf "${TEMPLATE_DIR}"

	echo "Packaging disk image"
	hdiutil create "${DMG_PATH}" -format UDRW -volname "OpenRA" -fs HFS+ -srcfolder build
	DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "${DMG_PATH}" | egrep '^/dev/' | sed 1q | awk '{print $1}')
	sleep 2

	# Background image is created from source svg in artsrc repository
	mkdir "/Volumes/OpenRA/.background/"
	tiffutil -cathidpicheck "${ARTWORK_DIR}/macos-background.png" "${ARTWORK_DIR}/macos-background-2x.png" -out "/Volumes/OpenRA/.background/background.tiff"

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
	rm -rf "${BUILTDIR}"
}

notarize_package() {
	DMG_PATH="${1}"
	NOTARIZE_DMG_PATH="${DMG_PATH%.*}"-notarization.dmg
	echo "Submitting ${DMG_PATH} for notarization"

	# Reset xcode search path to fix xcrun not finding altool
	sudo xcode-select -r

	# Create a temporary read-only dmg for submission (notarization service rejects read/write images)
	hdiutil convert "${DMG_PATH}" -format UDZO -imagekey zlib-level=9 -ov -o "${NOTARIZE_DMG_PATH}"

	NOTARIZATION_UUID=$(xcrun altool --notarize-app --primary-bundle-id "net.openra.packaging" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" --file "${NOTARIZE_DMG_PATH}" 2>&1 | awk -F' = ' '/RequestUUID/ { print $2; exit }')
	if [ -z "${NOTARIZATION_UUID}" ]; then
		echo "Submission failed"
		exit 1
	fi

	echo "${DMG_PATH} submission UUID is ${NOTARIZATION_UUID}"
	rm "${NOTARIZE_DMG_PATH}"

	while :; do
		sleep 30
		NOTARIZATION_RESULT=$(xcrun altool --notarization-info "${NOTARIZATION_UUID}" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" 2>&1 | awk -F': ' '/Status/ { print $2; exit }')
		echo "${DMG_PATH}: ${NOTARIZATION_RESULT}"

		if [ "${NOTARIZATION_RESULT}" == "invalid" ]; then
			NOTARIZATION_LOG_URL=$(xcrun altool --notarization-info "${NOTARIZATION_UUID}" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" 2>&1 | awk -F': ' '/LogFileURL/ { print $2; exit }')
			echo "${NOTARIZATION_UUID} failed notarization with error:"
			curl -s "${NOTARIZATION_LOG_URL}" -w "\n"
			exit 1
		fi

		if [ "${NOTARIZATION_RESULT}" == "success" ]; then
			echo "${DMG_PATH}: Stapling tickets"
			DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "${DMG_PATH}" | egrep '^/dev/' | sed 1q | awk '{print $1}')
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
}

finalize_package() {
	INPUT_PATH="${1}"
	OUTPUT_PATH="${2}"

	hdiutil convert "${INPUT_PATH}" -format UDZO -imagekey zlib-level=9 -ov -o "${OUTPUT_PATH}"
	rm "${INPUT_PATH}"
}

build_platform "standard" "build.dmg"
build_platform "compat" "build-compat.dmg"

if [ -n "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" ] && [ -n "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	security delete-keychain build.keychain
fi

if [ -n "${MACOS_DEVELOPER_USERNAME}" ] && [ -n "${MACOS_DEVELOPER_PASSWORD}" ]; then
	# Parallelize processing
	(notarize_package "build.dmg") &
	(notarize_package "build-compat.dmg") &
	wait
fi

finalize_package "build.dmg" "${OUTPUTDIR}/OpenRA-${TAG}.dmg"
finalize_package "build-compat.dmg" "${OUTPUTDIR}/OpenRA-${TAG}-compat.dmg"
