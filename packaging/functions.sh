#!/bin/sh
# Helper functions for packaging and installing OpenRA

####
# This file must stay /bin/sh and POSIX compliant for macOS and BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# Compile and publish (using Mono) the core engine and specified mod assemblies to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   DEST_PATH: Path to the root of the install destination (will be created if necessary)
#   TARGETPLATFORM: Platform type (win-x86, win-x64, osx-x64, linux-x64, unix-generic)
#   COPY_GENERIC_LAUNCHER: If set to True the OpenRA.exe will also be copied (True, False)
#   COPY_CNC_DLL: If set to True the OpenRA.Mods.Cnc.dll will also be copied (True, False)
#   COPY_D2K_DLL: If set to True the OpenRA.Mods.D2k.dll will also be copied (True, False)
# Used by:
#   Makefile (install target for local installs and downstream packaging)
#   Linux AppImage packaging
#   macOS packaging
#   Windows packaging
#   Mod SDK Linux AppImage packaging
#   Mod SDK macOS packaging
#   Mod SDK Windows packaging
install_assemblies_mono() {
	SRC_PATH="${1}"
	DEST_PATH="${2}"
	TARGETPLATFORM="${3}"
	COPY_GENERIC_LAUNCHER="${4}"
	COPY_CNC_DLL="${5}"
	COPY_D2K_DLL="${6}"

	echo "Building assemblies"
	ORIG_PWD=$(pwd)
	cd "${SRC_PATH}" || exit 1
	msbuild -verbosity:m -nologo -t:Clean
	rm -rf "${SRC_PATH:?}/bin"
	msbuild -verbosity:m -nologo -t:Build -restore -p:Configuration=Release -p:TargetPlatform="${TARGETPLATFORM}"
	if [ "${TARGETPLATFORM}" = "unix-generic" ]; then
		./configure-system-libraries.sh
	fi

	./fetch-geoip.sh
	cd "${ORIG_PWD}" || exit 1

	echo "Installing engine to ${DEST_PATH}"
	install -d "${DEST_PATH}"

	# Core engine
	install -m755 "${SRC_PATH}/bin/OpenRA.Server.exe" "${DEST_PATH}"
	install -m755 "${SRC_PATH}/bin/OpenRA.Utility.exe" "${DEST_PATH}"
	install -m644 "${SRC_PATH}/bin/OpenRA.Game.dll" "${DEST_PATH}"
	install -m644 "${SRC_PATH}/bin/OpenRA.Platforms.Default.dll" "${DEST_PATH}"
	if [ "${COPY_GENERIC_LAUNCHER}" = "True" ]; then
		install -m755 "${SRC_PATH}/bin/OpenRA.exe" "${DEST_PATH}"
	fi

	# Mod dlls
	install -m644 "${SRC_PATH}/bin/OpenRA.Mods.Common.dll" "${DEST_PATH}"
	if [ "${COPY_CNC_DLL}" = "True" ]; then
		install -m644 "${SRC_PATH}/bin/OpenRA.Mods.Cnc.dll" "${DEST_PATH}"
	fi

	if [ "${COPY_D2K_DLL}" = "True" ]; then
		install -m644 "${SRC_PATH}/bin/OpenRA.Mods.D2k.dll" "${DEST_PATH}"
	fi

	# Managed Dependencies
	for LIB in ICSharpCode.SharpZipLib.dll FuzzyLogicLibrary.dll Open.Nat.dll BeaconLib.dll DiscordRPC.dll Newtonsoft.Json.dll SDL2-CS.dll OpenAL-CS.Core.dll Eluant.dll; do
		install -m644 "${SRC_PATH}/bin/${LIB}" "${DEST_PATH}"
	done

	# Native dependencies
	if [ "${TARGETPLATFORM}" = "win-x86" ] || [ "${TARGETPLATFORM}" = "win-x64" ]; then
		echo "Installing dependencies for ${TARGETPLATFORM} to ${DEST_PATH}"
		for LIB in soft_oal.dll SDL2.dll freetype6.dll lua51.dll libEGL.dll libGLESv2.dll; do
			install -m644 "${SRC_PATH}/bin/${LIB}" "${DEST_PATH}"
		done
	else
		for LIB in OpenRA.Platforms.Default.dll.config SDL2-CS.dll.config OpenAL-CS.Core.dll.config Eluant.dll.config; do
			install -m644 "${SRC_PATH}/bin/${LIB}" "${DEST_PATH}"
		done
	fi

	if [ "${TARGETPLATFORM}" = "linux-x64" ]; then
		echo "Installing dependencies for ${TARGETPLATFORM} to ${DEST_PATH}"
		for LIB in soft_oal.so SDL2.so freetype6.so lua51.so; do
			install -m755 "${SRC_PATH}/bin/${LIB}" "${DEST_PATH}"
		done
	fi

	if [ "${TARGETPLATFORM}" = "osx-x64" ]; then
		echo "Installing dependencies for ${TARGETPLATFORM} to ${DEST_PATH}"
		for LIB in soft_oal.dylib SDL2.dylib freetype6.dylib lua51.dylib; do
			install -m755 "${SRC_PATH}/bin/${LIB}" "${DEST_PATH}"
		done
	fi
}

# Copy the core engine and specified mod data to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   DEST_PATH: Path to the root of the install destination (will be created if necessary)
#   MOD [MOD...]: One or more mod ids to copy (cnc, d2k, ra)
# Used by:
#   Makefile (install target for local installs and downstream packaging)
#   Linux AppImage packaging
#   macOS packaging
#   Windows packaging
#   Mod SDK Linux AppImage packaging
#   Mod SDK macOS packaging
#   Mod SDK Windows packaging
install_data() {
	SRC_PATH="${1}"
	DEST_PATH="${2}"
	shift 2

	echo "Installing engine files to ${DEST_PATH}"
	for FILE in VERSION AUTHORS COPYING IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP "global mix database.dat"; do
		install -m644 "${SRC_PATH}/${FILE}" "${DEST_PATH}"
	done

	cp -r "${SRC_PATH}/glsl" "${DEST_PATH}"
	cp -r "${SRC_PATH}/lua" "${DEST_PATH}"

	echo "Installing common mod files to ${DEST_PATH}"
	install -d "${DEST_PATH}/mods"
	cp -r "${SRC_PATH}/mods/common" "${DEST_PATH}/mods/"

	while [ -n "${1}" ]; do
		MOD_ID="${1}"
		if [ "${MOD_ID}" = "ra" ] || [ "${MOD_ID}" = "cnc" ] || [ "${MOD_ID}" = "d2k" ]; then
			echo "Installing mod ${MOD_ID} to ${DEST_PATH}"
			cp -r "${SRC_PATH}/mods/${MOD_ID}" "${DEST_PATH}/mods/"
			cp -r "${SRC_PATH}/mods/modcontent" "${DEST_PATH}/mods/"
		fi

		shift
	done
}

# Compile and publish (using Mono) a windows launcher with the specified mod details to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   DEST_PATH: Path to the root of the install destination (will be created if necessary)
#   TARGETPLATFORM: Platform type (win-x86, win-x64)
#   MOD_ID: Mod id to launch (e.g. "ra")
#   LAUNCHER_NAME: Filename (without the .exe extension) for the launcher
#   MOD_NAME: Human-readable mod name to show in the crash dialog (e.g. "Red Alert")
#   ICON_PATH: Path to a windows .ico file
#   FAQ_URL: URL to load when the "View FAQ" button is pressed in the crash dialog (e.g. https://wiki.openra.net/FAQ)
# Used by:
#   Windows packaging
#   Mod SDK Windows packaging
install_windows_launcher()
{
	SRC_PATH="${1}"
	DEST_PATH="${2}"
	TARGETPLATFORM="${3}"
	MOD_ID="${4}"
	LAUNCHER_NAME="${5}"
	MOD_NAME="${6}"
	ICON_PATH="${7}"
	FAQ_URL="${8}"

	msbuild -verbosity:m -nologo -t:Clean "${SRC_PATH}/OpenRA.WindowsLauncher/OpenRA.WindowsLauncher.csproj"
	rm -rf "${SRC_PATH:?}/bin"
	msbuild -t:Build "${SRC_PATH}/OpenRA.WindowsLauncher/OpenRA.WindowsLauncher.csproj" -restore -p:Configuration=Release -p:TargetPlatform="${TARGETPLATFORM}" -p:LauncherName="${LAUNCHER_NAME}" -p:LauncherIcon="${ICON_PATH}" -p:ModID="${MOD_ID}" -p:DisplayName="${MOD_NAME}" -p:FaqUrl="${FAQ_URL}"
	install -m755 "${SRC_PATH}/bin/${LAUNCHER_NAME}.exe" "${DEST_PATH}"
	install -m644 "${SRC_PATH}/bin/${LAUNCHER_NAME}.exe.config" "${DEST_PATH}"

	# Enable the full 4GB address space for the 32 bit game executable
	# The server and utility do not use enough memory to need this
	if [ "${TARGETPLATFORM}" = "win-x86" ]; then
		python3 "${SRC_PATH}/packaging/windows/MakeLAA.py" "${DEST_PATH}/${LAUNCHER_NAME}.exe"
	fi
}

# Write a version string to the engine VERSION file
# Arguments:
#   VERSION: OpenRA version string
#   DEST_PATH: Path to the root of the install destination
# Used by:
#   Makefile (install target for local installs and downstream packaging)
#   Linux AppImage packaging
#   macOS packaging
#   Windows packaging
#   Mod SDK Linux AppImage packaging
#   Mod SDK macOS packaging
#   Mod SDK Windows packaging
set_engine_version() {
	VERSION="${1}"
	DEST_PATH="${2}"
	echo "${VERSION}" > "${DEST_PATH}/VERSION"
}

# Write a version string to a list of specified mod.yamls
# Arguments:
#   VERSION: OpenRA version string
#   MOD_YAML_PATH [MOD_YAML_PATH...]: One or more mod.yaml files to update
# Used by:
#   Makefile (install target for local installs and downstream packaging)
#   Linux AppImage packaging
#   macOS packaging
#   Windows packaging
#   Mod SDK Linux AppImage packaging
#   Mod SDK macOS packaging
#   Mod SDK Windows packaging
set_mod_version() {
	VERSION="${1}"
	shift
	while [ -n "${1}" ]; do
		MOD_YAML_PATH="${1}"
		awk -v v="${VERSION}" '{sub("Version:.*$", "Version: " v); print $0}' "${MOD_YAML_PATH}" > "${MOD_YAML_PATH}.tmp"
		awk -v v="${VERSION}" '{sub("/[^/]*: User$", "/"v ": User"); print $0}' "${MOD_YAML_PATH}.tmp" > "${MOD_YAML_PATH}"
		rm "${MOD_YAML_PATH}.tmp"
		shift
	done
}

# Copy launch wrappers, application icons, desktop, and MIME files to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   BUILD_PATH: Path to packaging filesystem root (e.g. /tmp/openra-build/ or "" for a local install)
#   OPENRA_PATH: Path to the OpenRA installation (e.g. /usr/local/lib/openra)
#   BIN_PATH: Path to install wrapper scripts (e.g. /usr/local/bin)
#   SHARE_PATH: Parent path to the icons and applications directory (e.g. /usr/local/share)
#   VERSION: OpenRA version string
#   MOD [MOD...]: One or more mod ids to copy (cnc, d2k, ra)
# Used by:
#   Makefile (install-linux-shortcuts target for local installs and downstream packaging)
install_linux_shortcuts() {
	SRC_PATH="${1}"
	BUILD_PATH="${2}"
	OPENRA_PATH="${3}"
	BIN_PATH="${4}"
	SHARE_PATH="${5}"
	VERSION="${6}"
	shift 6

	while [ -n "${1}" ]; do
		MOD_ID="${1}"
		if [ "${MOD_ID}" = "ra" ] || [ "${MOD_ID}" = "cnc" ] || [ "${MOD_ID}" = "d2k" ]; then
			if [ "${MOD_ID}" = "cnc" ]; then
				MOD_NAME="Tiberian Dawn"
			fi

			if [ "${MOD_ID}" = "d2k" ]; then
				MOD_NAME="Dune 2000"
			fi

			if [ "${MOD_ID}" = "ra" ]; then
				MOD_NAME="Red Alert"
			fi

			# wrapper scripts
			install -d "${BUILD_PATH}/${BIN_PATH}"
			sed 's/{DEBUG}/--debug/' "${SRC_PATH}/packaging/linux/openra.in" | sed "s|{GAME_INSTALL_DIR}|${OPENRA_PATH}|" | sed "s|{BIN_DIR}|${BIN_PATH}|" | sed "s/{MODID}/${MOD_ID}/g" | sed "s/{TAG}/${VERSION}/g" | sed "s/{MODNAME}/${MOD_NAME}/g" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}"
			sed 's/{DEBUG}/--debug/' "${SRC_PATH}/packaging/linux/openra-server.in" | sed "s|{GAME_INSTALL_DIR}|${OPENRA_PATH}|" | sed "s/{MODID}/${MOD_ID}/g" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}-server"
			install -m755 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}" "${BUILD_PATH}/${BIN_PATH}"
			install -m755 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}-server" "${BUILD_PATH}/${BIN_PATH}"
			rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}" "${SRC_PATH}/packaging/linux/openra-${MOD_ID}-server"

			# desktop files
			install -d "${BUILD_PATH}${SHARE_PATH}/applications"
			sed "s/{MODID}/${MOD_ID}/g" "${SRC_PATH}/packaging/linux/openra.desktop.in" | sed "s/{MODNAME}/${MOD_NAME}/g" | sed "s/{TAG}/${VERSION}/g" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.desktop"
			install -m644 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.desktop" "${BUILD_PATH}${SHARE_PATH}/applications"
			rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.desktop"

			# icons
			for SIZE in 16x16 32x32 48x48 64x64 128x128; do
				install -d "${BUILD_PATH}${SHARE_PATH}/icons/hicolor/${SIZE}/apps"
				install -m644 "${SRC_PATH}/packaging/artwork/${MOD_ID}_${SIZE}.png" "${BUILD_PATH}${SHARE_PATH}/icons/hicolor/${SIZE}/apps/openra-${MOD_ID}.png"
			done

			if [ "${MOD_ID}" = "ra" ] || [ "${MOD_ID}" = "cnc" ]; then
				install -d "${BUILD_PATH}${SHARE_PATH}/icons/hicolor/scalable/apps"
				install -m644 "${SRC_PATH}/packaging/artwork/${MOD_ID}_scalable.svg" "${BUILD_PATH}${SHARE_PATH}/icons/hicolor/scalable/apps/openra-${MOD_ID}.svg"
			fi

			# MIME info
			install -d "${BUILD_PATH}${SHARE_PATH}/mime/packages"
			sed "s/{MODID}/${MOD_ID}/g" "${SRC_PATH}/packaging/linux/openra-mimeinfo.xml.in" | sed "s/{TAG}/${VERSION}/g" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.xml"
			install -m644 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.xml" "${BUILD_PATH}${SHARE_PATH}/mime/packages/openra-${MOD_ID}.xml"
			rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.xml"
		fi

		shift
	done
}

# Copy AppStream metadata to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   BUILD_PATH: Path to packaging filesystem root (e.g. /tmp/openra-build/ or "" for a local install)
#   SHARE_PATH: Parent path to the appdata directory (e.g. /usr/local/share)
#   MOD [MOD...]: One or more mod ids to copy (cnc, d2k, ra)
# Used by:
#   Makefile (install-linux-appdata target for local installs and downstream packaging)
install_linux_appdata() {
	SRC_PATH="${1}"
	BUILD_PATH="${2}"
	SHARE_PATH="${3}"
	shift 3
	while [ -n "${1}" ]; do
		MOD_ID="${1}"
		SCREENSHOT_CNC=
		SCREENSHOT_D2K=
		SCREENSHOT_RA=
		if [ "${MOD_ID}" = "ra" ] || [ "${MOD_ID}" = "cnc" ] || [ "${MOD_ID}" = "d2k" ]; then
			if [ "${MOD_ID}" = "cnc" ]; then
				MOD_NAME="Tiberian Dawn"
				SCREENSHOT_CNC=" type=\"default\""
			fi

			if [ "${MOD_ID}" = "d2k" ]; then
				MOD_NAME="Dune 2000"
				SCREENSHOT_D2K=" type=\"default\""
			fi

			if [ "${MOD_ID}" = "ra" ]; then
				MOD_NAME="Red Alert"
				SCREENSHOT_RA=" type=\"default\""
			fi
		fi

		install -d "${BUILD_PATH}${SHARE_PATH}/metainfo"

		sed "s/{MODID}/${MOD_ID}/g" "${SRC_PATH}/packaging/linux/openra.metainfo.xml.in" | sed "s/{MOD_NAME}/${MOD_NAME}/g" | sed "s/{SCREENSHOT_RA}/${SCREENSHOT_RA}/g" | sed "s/{SCREENSHOT_CNC}/${SCREENSHOT_CNC}/g" | sed "s/{SCREENSHOT_D2K}/${SCREENSHOT_D2K}/g"> "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.metainfo.xml"
		install -m644 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.metainfo.xml" "${BUILD_PATH}${SHARE_PATH}/metainfo"
		rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.metainfo.xml"

		shift
	done
}
