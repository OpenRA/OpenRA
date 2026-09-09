#!/bin/bash
# OpenRA packaging script for Linux (AppImage)

set -o errexit -o pipefail || exit $?

command -v tar >/dev/null 2>&1 || { echo >&2 "Linux packaging requires tar."; exit 1; }
command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "Linux packaging requires curl or wget."; exit 1; }

DEPENDENCIES_TAG="20201222"

if [ $# -eq "0" ]; then
	echo "Usage: $(basename "$0") version [outputdir]"
	exit 1
fi

# Set the working dir to the location of this script
HERE=$(dirname "$0")
cd "${HERE}"
. ../functions.sh

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
ARTWORK_DIR="$(pwd)/../artwork/"

UPDATE_CHANNEL=""
SUFFIX="-devel"
EMBED_UPDATE_METADATA="True"
if [[ ${TAG} == release* ]]; then
	UPDATE_CHANNEL="release"
	SUFFIX=""
elif [[ ${TAG} == playtest* ]]; then
	UPDATE_CHANNEL="playtest"
	SUFFIX="-playtest"
elif [[ ${TAG} == pkgtest* ]]; then
	UPDATE_CHANNEL="pkgtest"
	SUFFIX="-pkgtest"
elif [[ ${TAG} == pvphit* ]]; then
	# PvPHit release tags (pvphit-20250330.N, see PVPHIT.md) are release builds:
	# the artifact contract name has no suffix, and they are not served by the
	# OpenRA update channels, so no zsync metadata is embedded.
	SUFFIX=""
	EMBED_UPDATE_METADATA="False"
fi

# TEMPLATE_ROOT is never set (upstream vestige); default to "." because bash >= 5.2 rejects pushd ""
pushd "${TEMPLATE_ROOT:-.}" > /dev/null

if [ ! -d "${OUTPUTDIR}" ]; then
	echo "Output directory '${OUTPUTDIR}' does not exist.";
	exit 1
fi

# Add native libraries
echo "Downloading appimagetool"
if command -v curl >/dev/null 2>&1; then
	curl -s -L -O https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
else
	wget -cq https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
fi

chmod a+x appimagetool-x86_64.AppImage

echo "Building AppImages"

build_appimage() {
	local MOD_ID DISPLAY_NAME DISCORD_ID APPDIR APPIMAGE IS_D2K ART_ID DESKTOP_TEMPLATE MIMEINFO_TEMPLATE
	MOD_ID=${1}
	DISPLAY_NAME=${2}
	DISCORD_ID=${3}
	APPDIR="$(pwd)/${MOD_ID}.appdir"
	APPIMAGE="OpenRA-$(echo "${DISPLAY_NAME}" | sed 's/ /-/g')${SUFFIX}-x86_64.AppImage"

	ART_ID="${MOD_ID}"
	if [ "${MOD_ID}" = "pvphit" ]; then
		# Reuse the Red Alert artwork until the PvPHit art batch (O7) lands
		ART_ID="ra"
	fi

	# Mods without a Discord application id must not register a discord-* URI
	# handler, so use the templates without the Discord scheme for them.
	DESKTOP_TEMPLATE="openra.desktop.discord.in"
	MIMEINFO_TEMPLATE="openra-mimeinfo.xml.discord.in"
	if [ -z "${DISCORD_ID}" ]; then
		DESKTOP_TEMPLATE="openra.desktop.in"
		MIMEINFO_TEMPLATE="openra-mimeinfo.xml.in"
	fi

	IS_D2K="False"
	if [ "${MOD_ID}" = "d2k" ]; then
		IS_D2K="True"
	fi

	install_assemblies "${SRCDIR}" "${APPDIR}/usr/lib/openra" "linux-x64" "net6" "True" "True" "${IS_D2K}"
	install_data "${SRCDIR}" "${APPDIR}/usr/lib/openra" "${MOD_ID}"
	set_engine_version "${TAG}" "${APPDIR}/usr/lib/openra"
	if [ "${MOD_ID}" = "pvphit" ]; then
		# The mod, its content installer and the ra packages it inherits must advertise the same version
		set_mod_version "${TAG}" "${APPDIR}/usr/lib/openra/mods/pvphit/mod.yaml" "${APPDIR}/usr/lib/openra/mods/pvphit-content/mod.yaml" "${APPDIR}/usr/lib/openra/mods/ra/mod.yaml" "${APPDIR}/usr/lib/openra/mods/ra-content/mod.yaml"
	else
		set_mod_version "${TAG}" "${APPDIR}/usr/lib/openra/mods/${MOD_ID}/mod.yaml" "${APPDIR}/usr/lib/openra/mods/${MOD_ID}-content/mod.yaml"
	fi

	# Add launcher and icons
	sed "s/{MODID}/${MOD_ID}/g" AppRun.in | sed "s/{MODNAME}/${DISPLAY_NAME}/g" > "${APPDIR}/AppRun"
	chmod 0755 "${APPDIR}/AppRun"

	mkdir -p "${APPDIR}/usr/share/applications"
	# Note that the non-discord version of the desktop file is used by the Mod SDK and must be maintained in parallel with the discord version!
	sed "s/{MODID}/${MOD_ID}/g" "${DESKTOP_TEMPLATE}" | sed "s/{MODNAME}/${DISPLAY_NAME}/g" | sed "s/{TAG}/${TAG}/g" | sed "s/{DISCORDAPPID}/${DISCORD_ID}/g" > "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
	chmod 0755 "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
	cp "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop" "${APPDIR}/openra-${MOD_ID}.desktop"

	mkdir -p "${APPDIR}/usr/share/mime/packages"
	# Note that the non-discord version of the mimeinfo file is used by the Mod SDK and must be maintained in parallel with the discord version!
	sed "s/{MODID}/${MOD_ID}/g" "${MIMEINFO_TEMPLATE}" | sed "s/{TAG}/${TAG}/g" | sed "s/{DISCORDAPPID}/${DISCORD_ID}/g" > "${APPDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"
	chmod 0755 "${APPDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"

	if [ -f "${ARTWORK_DIR}/${ART_ID}_scalable.svg" ]; then
		install -Dm644 "${ARTWORK_DIR}/${ART_ID}_scalable.svg" "${APPDIR}/usr/share/icons/hicolor/scalable/apps/openra-${MOD_ID}.svg"
	fi

	for i in 16x16 32x32 48x48 64x64 128x128 256x256 512x512 1024x1024; do
		if [ -f "${ARTWORK_DIR}/${ART_ID}_${i}.png" ]; then
			install -Dm644 "${ARTWORK_DIR}/${ART_ID}_${i}.png" "${APPDIR}/usr/share/icons/hicolor/${i}/apps/openra-${MOD_ID}.png"
			install -m644 "${ARTWORK_DIR}/${ART_ID}_${i}.png" "${APPDIR}/openra-${MOD_ID}.png"
		fi
	done

	mkdir -p "${APPDIR}/usr/bin"
	sed "s/{MODID}/${MOD_ID}/g" openra.appimage.in | sed "s/{TAG}/${TAG}/g" | sed "s/{MODNAME}/${DISPLAY_NAME}/g" > "${APPDIR}/usr/bin/openra-${MOD_ID}"
	chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}"

	sed "s/{MODID}/${MOD_ID}/g" openra-server.appimage.in > "${APPDIR}/usr/bin/openra-${MOD_ID}-server"
	chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}-server"

	sed "s/{MODID}/${MOD_ID}/g" openra-utility.appimage.in > "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"
	chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"

	install -m 0755 gtk-dialog.py "${APPDIR}/usr/bin/gtk-dialog.py"

	# Embed update metadata if (and only if) compiled on GitHub Actions for an OpenRA update channel
	if [ -n "${GITHUB_REPOSITORY}" ] && [ "${EMBED_UPDATE_METADATA}" = "True" ]; then
		ARCH=x86_64 ./appimagetool-x86_64.AppImage --no-appstream -u "zsync|https://master.openra.net/appimagecheck.zsync?mod=${MOD_ID}&channel=${UPDATE_CHANNEL}" "${APPDIR}" "${OUTPUTDIR}/${APPIMAGE}"
		zsyncmake -u "https://github.com/${GITHUB_REPOSITORY}/releases/download/${TAG}/${APPIMAGE}" -o "${OUTPUTDIR}/${APPIMAGE}.zsync" "${OUTPUTDIR}/${APPIMAGE}"
	else
		ARCH=x86_64 ./appimagetool-x86_64.AppImage --no-appstream "${APPDIR}" "${OUTPUTDIR}/${APPIMAGE}"
	fi

	rm -rf "${APPDIR}"
}

build_appimage "ra" "Red Alert" "699222659766026240"
build_appimage "cnc" "Tiberian Dawn" "699223250181292033"
build_appimage "d2k" "Dune 2000" "712711732770111550"
# PvPHit: Red Alert data + PvPHit identity (pabl-o-ce/pvphit docs/openra/).
# No Discord application id yet: passing "" keeps the pvphit AppImage from
# registering Red Alert's discord-* URI handler. Pass the PvPHit id here (and
# restore DiscordService in mods/pvphit/mod.yaml) once one is registered.
build_appimage "pvphit" "PvPHit" ""

# Clean up
rm -rf appimagetool-x86_64.AppImage "${BUILTDIR}"
