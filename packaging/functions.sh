#!/bin/sh
# Helper functions for packaging and installing OpenRA

####
# This file must stay /bin/sh and POSIX compliant for macOS and BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# Compile and publish the core engine and specified mod assemblies to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   DEST_PATH: Path to the root of the install destination (will be created if necessary)
#   TARGETPLATFORM: Platform type (win-x86, win-x64, osx-x64, osx-arm64, linux-x64, linux-arm64, unix-generic)
#   RUNTIME: Runtime type (net6, mono)
#   COPY_GENERIC_LAUNCHER: If set to True the OpenRA.exe will also be copied (True, False)
#   COPY_CNC_DLL: If set to True the OpenRA.Mods.Cnc.dll will also be copied (True, False)
#   COPY_D2K_DLL: If set to True the OpenRA.Mods.D2k.dll will also be copied (True, False)
# Used by:
#   Makefile (install target for local installs and downstream packaging)
#   Windows packaging
#   macOS packaging
#   Linux AppImage packaging
#   Mod SDK Windows packaging
#   Mod SDK macOS packaging
#   Mod SDK Linux AppImage packaging
install_assemblies() (
	set -o errexit || exit $?

	SRC_PATH="${1}"
	DEST_PATH="${2}"
	TARGETPLATFORM="${3}"
	RUNTIME="${4}"
	COPY_GENERIC_LAUNCHER="${5}"
	COPY_CNC_DLL="${6}"
	COPY_D2K_DLL="${7}"

	ORIG_PWD=$(pwd)
	cd "${SRC_PATH}"

	if [ "${RUNTIME}" = "mono" ]; then
		echo "Building assemblies"
		rm -rf "${SRC_PATH}/OpenRA."*/obj || :
		rm -rf "${SRC_PATH:?}/bin" || :

		msbuild -verbosity:m -nologo -t:Build -restore -p:Configuration=Release -p:TargetPlatform="${TARGETPLATFORM}"
		if [ "${TARGETPLATFORM}" = "unix-generic" ]; then
			./configure-system-libraries.sh
		fi

		if [ "${COPY_GENERIC_LAUNCHER}" != "True" ]; then
			rm "${SRC_PATH}/bin/OpenRA.dll"
		fi

		if [ "${COPY_CNC_DLL}" != "True" ]; then
			rm "${SRC_PATH}/bin/OpenRA.Mods.Cnc.dll"
		fi

		if [ "${COPY_D2K_DLL}" != "True" ]; then
			rm "${SRC_PATH}/bin/OpenRA.Mods.D2k.dll"
		fi

		cd "${ORIG_PWD}"

		echo "Installing engine to ${DEST_PATH}"
		install -d "${DEST_PATH}"

		for LIB in "${SRC_PATH}/bin/"*.dll "${SRC_PATH}/bin/"*.dll.config; do
			install -m644 "${LIB}" "${DEST_PATH}"
		done

		if [ "${TARGETPLATFORM}" = "linux-x64" ] || [ "${TARGETPLATFORM}" = "linux-arm64" ]; then
			for LIB in "${SRC_PATH}/bin/"*.so; do
				install -m755 "${LIB}" "${DEST_PATH}"
			done
		fi

		if [ "${TARGETPLATFORM}" = "osx-x64" ] || [ "${TARGETPLATFORM}" = "osx-arm64" ]; then
			for LIB in "${SRC_PATH}/bin/"*.dylib; do
				install -m755 "${LIB}" "${DEST_PATH}"
			done
		fi
	else
		dotnet publish -c Release -p:TargetPlatform="${TARGETPLATFORM}" -p:CopyGenericLauncher="${COPY_GENERIC_LAUNCHER}" -p:CopyCncDll="${COPY_CNC_DLL}" -p:CopyD2kDll="${COPY_D2K_DLL}" -r "${TARGETPLATFORM}" -p:PublishDir="${DEST_PATH}" --self-contained true
	fi
	cd "${ORIG_PWD}"
)

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
install_data() (
	set -o errexit || exit $?

	SRC_PATH="${1}"
	DEST_PATH="${2}"
	shift 2

	"${SRC_PATH}"/fetch-geoip.sh

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
)

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
install_windows_launcher() (
	set -o errexit || exit $?

	SRC_PATH="${1}"
	DEST_PATH="${2}"
	TARGETPLATFORM="${3}"
	MOD_ID="${4}"
	LAUNCHER_NAME="${5}"
	MOD_NAME="${6}"
	FAQ_URL="${7}"

	rm -rf "${SRC_PATH}/OpenRA.WindowsLauncher/obj" || :
	dotnet publish "${SRC_PATH}/OpenRA.WindowsLauncher/OpenRA.WindowsLauncher.csproj" -c Release -r "${TARGETPLATFORM}" -p:LauncherName="${LAUNCHER_NAME}" -p:TargetPlatform="${TARGETPLATFORM}" -p:ModID="${MOD_ID}" -p:DisplayName="${MOD_NAME}" -p:FaqUrl="${FAQ_URL}" -p:PublishDir="${DEST_PATH}" --self-contained true

	# NET 6 is unable to customize the application host for windows when compiling from Linux,
	# so we must patch the properties we need in the PE header.
	# Setting the application icon requires an external tool, so is left to the calling code
	python3 "${SRC_PATH}/packaging/windows/fixlauncher.py" "${DEST_PATH}/${LAUNCHER_NAME}.exe"
)

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
set_engine_version() (
	set -o errexit || exit $?

	VERSION="${1}"
	DEST_PATH="${2}"
	echo "${VERSION}" > "${DEST_PATH}/VERSION"
)

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
set_mod_version() (
	set -o errexit || exit $?

	VERSION="${1}"
	shift
	while [ -n "${1}" ]; do
		MOD_YAML_PATH="${1}"
		awk -v v="${VERSION}" '{sub("Version:.*$", "Version: " v); print $0}' "${MOD_YAML_PATH}" > "${MOD_YAML_PATH}.tmp"
		awk -v v="${VERSION}" '{sub("/[^/]*: User$", "/"v ": User"); print $0}' "${MOD_YAML_PATH}.tmp" > "${MOD_YAML_PATH}"
		rm "${MOD_YAML_PATH}.tmp"
		shift
	done
)

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
install_linux_shortcuts() (
	set -o errexit || exit $?

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
			sed -e 's/{DEBUG}/--debug/' -e "s|{GAME_INSTALL_DIR}|${OPENRA_PATH}|" -e "s|{BIN_DIR}|${BIN_PATH}|" -e "s/{MODID}/${MOD_ID}/g" -e "s/{TAG}/${VERSION}/g" -e "s/{MODNAME}/${MOD_NAME}/g" "${SRC_PATH}/packaging/linux/openra.in" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}"
			sed -e 's/{DEBUG}/--debug/' -e "s|{GAME_INSTALL_DIR}|${OPENRA_PATH}|" -e "s/{MODID}/${MOD_ID}/g" "${SRC_PATH}/packaging/linux/openra-server.in" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}-server"
			install -m755 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}" "${BUILD_PATH}/${BIN_PATH}"
			install -m755 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}-server" "${BUILD_PATH}/${BIN_PATH}"
			rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}" "${SRC_PATH}/packaging/linux/openra-${MOD_ID}-server"

			# desktop files
			install -d "${BUILD_PATH}${SHARE_PATH}/applications"
			sed -e "s/{MODID}/${MOD_ID}/g" -e "s/{MODNAME}/${MOD_NAME}/g" -e "s/{TAG}/${VERSION}/g" "${SRC_PATH}/packaging/linux/openra.desktop.in" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.desktop"
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
			sed -e "s/{MODID}/${MOD_ID}/g" -e "s/{TAG}/${VERSION}/g" "${SRC_PATH}/packaging/linux/openra-mimeinfo.xml.in" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.xml"
			install -m644 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.xml" "${BUILD_PATH}${SHARE_PATH}/mime/packages/openra-${MOD_ID}.xml"
			rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.xml"
		fi

		shift
	done
)

# Copy AppStream metadata to the target directory
# Arguments:
#   SRC_PATH: Path to the root OpenRA directory
#   BUILD_PATH: Path to packaging filesystem root (e.g. /tmp/openra-build/ or "" for a local install)
#   SHARE_PATH: Parent path to the appdata directory (e.g. /usr/local/share)
#   MOD [MOD...]: One or more mod ids to copy (cnc, d2k, ra)
# Used by:
#   Makefile (install-linux-appdata target for local installs and downstream packaging)
install_linux_appdata() (
	set -o errexit || exit $?

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

		sed -e "s/{MODID}/${MOD_ID}/g" -e "s/{MOD_NAME}/${MOD_NAME}/g" -e "s/{SCREENSHOT_RA}/${SCREENSHOT_RA}/g" -e "s/{SCREENSHOT_CNC}/${SCREENSHOT_CNC}/g" -e "s/{SCREENSHOT_D2K}/${SCREENSHOT_D2K}/g" "${SRC_PATH}/packaging/linux/openra.metainfo.xml.in" > "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.metainfo.xml"
		install -m644 "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.metainfo.xml" "${BUILD_PATH}${SHARE_PATH}/metainfo"
		rm "${SRC_PATH}/packaging/linux/openra-${MOD_ID}.metainfo.xml"

		shift
	done
)
