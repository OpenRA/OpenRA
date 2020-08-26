#!/bin/bash
# OpenRA packaging script for Linux (AppImage)
set -e

command -v tar >/dev/null 2>&1 || { echo >&2 "Linux packaging requires tar."; exit 1; }
command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "Linux packaging requires curl or wget."; exit 1; }

DEPENDENCIES_TAG="20201222"

if [ $# -eq "0" ]; then
	echo "Usage: $(basename "$0") version [outputdir]"
	exit 1
fi

# Set the working dir to the location of this script
cd "$(dirname "$0")" || exit 1
. ../functions.sh

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"
ARTWORK_DIR="$(pwd)/../artwork/"

UPDATE_CHANNEL=""
SUFFIX="-devel"
if [[ ${TAG} == release* ]]; then
	UPDATE_CHANNEL="release"
	SUFFIX=""
elif [[ ${TAG} == playtest* ]]; then
	UPDATE_CHANNEL="playtest"
	SUFFIX="-playtest"
elif [[ ${TAG} == pkgtest* ]]; then
	UPDATE_CHANNEL="pkgtest"
	SUFFIX="-pkgtest"
fi

pushd "${TEMPLATE_ROOT}" > /dev/null || exit 1

if [ ! -d "${OUTPUTDIR}" ]; then
	echo "Output directory '${OUTPUTDIR}' does not exist.";
	exit 1
fi

# Add native libraries
echo "Downloading dependencies"
if command -v curl >/dev/null 2>&1; then
	curl -s -L -O https://github.com/OpenRA/AppImageSupport/releases/download/${DEPENDENCIES_TAG}/mono.tar.bz2 || exit 3
	curl -s -L -O https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
else
	wget -cq https://github.com/OpenRA/AppImageSupport/releases/download/${DEPENDENCIES_TAG}/mono.tar.bz2 || exit 3
	wget -cq https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
fi

# travis-ci doesn't support mounting FUSE filesystems so extract and run the contents manually
chmod a+x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage --appimage-extract

echo "Building AppImage"
mkdir "${BUILTDIR}"
tar xf mono.tar.bz2 -C "${BUILTDIR}"
chmod 0755 "${BUILTDIR}/usr/bin/mono"
chmod 0644 "${BUILTDIR}/etc/mono/config"
chmod 0644 "${BUILTDIR}/etc/mono/4.5/machine.config"
chmod 0644 "${BUILTDIR}/usr/lib/mono/4.5/Facades/"*.dll
chmod 0644 "${BUILTDIR}/usr/lib/mono/4.5/"*.dll "${BUILTDIR}/usr/lib/mono/4.5/"*.exe
chmod 0755 "${BUILTDIR}/usr/lib/"*.so

rm -rf mono.tar.bz2

build_appimage() {
	MOD_ID=${1}
	DISPLAY_NAME=${2}
	DISCORD_ID=${3}
	APPDIR="$(pwd)/${MOD_ID}.appdir"
	APPIMAGE="OpenRA-$(echo "${DISPLAY_NAME}" | sed 's/ /-/g')${SUFFIX}-x86_64.AppImage"

	cp -r "${BUILTDIR}" "${APPDIR}"

	IS_D2K="False"
	if [ "${MOD_ID}" = "d2k" ]; then
		IS_D2K="True"
	fi

	install_assemblies_mono "${SRCDIR}" "${APPDIR}/usr/lib/openra" "linux-x64" "True" "True" "${IS_D2K}"
	install_data "${SRCDIR}" "${APPDIR}/usr/lib/openra" "${MOD_ID}"
	set_engine_version "${TAG}" "${APPDIR}/usr/lib/openra"
	set_mod_version "${TAG}" "${APPDIR}/usr/lib/openra/mods/${MOD_ID}/mod.yaml" "${APPDIR}/usr/lib/openra/mods/modcontent/mod.yaml"

	# Add launcher and icons
	sed "s/{MODID}/${MOD_ID}/g" AppRun.in | sed "s/{MODNAME}/${DISPLAY_NAME}/g" > "${APPDIR}/AppRun"
	chmod 0755 "${APPDIR}/AppRun"

	mkdir -p "${APPDIR}/usr/share/applications"
	# Note that the non-discord version of the desktop file is used by the Mod SDK and must be maintained in parallel with the discord version!
	sed "s/{MODID}/${MOD_ID}/g" openra.desktop.discord.in | sed "s/{MODNAME}/${DISPLAY_NAME}/g" | sed "s/{TAG}/${TAG}/g" | sed "s/{DISCORDAPPID}/${DISCORD_ID}/g" > "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
	chmod 0755 "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
	cp "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop" "${APPDIR}/openra-${MOD_ID}.desktop"

	mkdir -p "${APPDIR}/usr/share/mime/packages"
	# Note that the non-discord version of the mimeinfo file is used by the Mod SDK and must be maintained in parallel with the discord version!
	sed "s/{MODID}/${MOD_ID}/g" openra-mimeinfo.xml.discord.in | sed "s/{TAG}/${TAG}/g" | sed "s/{DISCORDAPPID}/${DISCORD_ID}/g" > "${APPDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"
	chmod 0755 "${APPDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"

	if [ -f "${ARTWORK_DIR}/${MOD_ID}_scalable.svg" ]; then
		install -Dm644 "${ARTWORK_DIR}/${MOD_ID}_scalable.svg" "${APPDIR}/usr/share/icons/hicolor/scalable/apps/openra-${MOD_ID}.svg"
	fi

	for i in 16x16 32x32 48x48 64x64 128x128 256x256 512x512 1024x1024; do
		if [ -f "${ARTWORK_DIR}/${MOD_ID}_${i}.png" ]; then
			install -Dm644 "${ARTWORK_DIR}/${MOD_ID}_${i}.png" "${APPDIR}/usr/share/icons/hicolor/${i}/apps/openra-${MOD_ID}.png"
			install -m644 "${ARTWORK_DIR}/${MOD_ID}_${i}.png" "${APPDIR}/openra-${MOD_ID}.png"
		fi
	done

	sed "s/{MODID}/${MOD_ID}/g" openra.appimage.in | sed "s/{TAG}/${TAG}/g" | sed "s/{MODNAME}/${DISPLAY_NAME}/g" > "${APPDIR}/usr/bin/openra-${MOD_ID}"
	chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}"

	sed "s/{MODID}/${MOD_ID}/g" openra-server.appimage.in > "${APPDIR}/usr/bin/openra-${MOD_ID}-server"
	chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}-server"

	sed "s/{MODID}/${MOD_ID}/g" openra-utility.appimage.in > "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"
	chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"

	install -m 0755 gtk-dialog.py "${APPDIR}/usr/bin/gtk-dialog.py"
	install -m 0755 restore-environment.sh "${APPDIR}/usr/bin/restore-environment.sh"

	# Embed update metadata if (and only if) compiled on GitHub Actions
	if [ -n "${GITHUB_REPOSITORY}" ]; then
		ARCH=x86_64 ./squashfs-root/AppRun --no-appstream -u "zsync|https://master.openra.net/appimagecheck?mod=${MOD_ID}&channel=${UPDATE_CHANNEL}" "${APPDIR}" "${OUTPUTDIR}/${APPIMAGE}"
		zsyncmake -u "https://github.com/${GITHUB_REPOSITORY}/releases/download/${TAG}/${APPIMAGE}" -o "${OUTPUTDIR}/${APPIMAGE}.zsync" "${OUTPUTDIR}/${APPIMAGE}"
	else
		ARCH=x86_64 ./squashfs-root/AppRun --no-appstream "${APPDIR}" "${OUTPUTDIR}/${APPIMAGE}"
	fi

	rm -rf "${APPDIR}"
}

build_appimage "ra" "Red Alert" "699222659766026240"
build_appimage "cnc" "Tiberian Dawn" "699223250181292033"
build_appimage "d2k" "Dune 2000" "712711732770111550"

# Clean up
rm -rf appimagetool-x86_64.AppImage squashfs-root "${BUILTDIR}"
