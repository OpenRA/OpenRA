#!/bin/bash
# OpenRA packaging script for Linux (AppImage)
set -e

command -v make >/dev/null 2>&1 || { echo >&2 "Linux packaging requires make."; exit 1; }
command -v python >/dev/null 2>&1 || { echo >&2 "Linux packaging requires python."; exit 1; }
command -v tar >/dev/null 2>&1 || { echo >&2 "Linux packaging requires tar."; exit 1; }
command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "Linux packaging requires curl or wget."; exit 1; }

DEPENDENCIES_TAG="20200328"

if [ $# -eq "0" ]; then
	echo "Usage: $(basename "$0") version [outputdir]"
	exit 1
fi

# Set the working dir to the location of this script
cd "$(dirname "$0")" || exit 1

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"

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

echo "Building core files"

pushd "${SRCDIR}" > /dev/null || exit 1

make clean

make core TARGETPLATFORM=linux-x64
make version VERSION="${TAG}"
make install-engine prefix="usr" DESTDIR="${BUILTDIR}/"
make install-common-mod-files prefix="usr" DESTDIR="${BUILTDIR}/"
make install-dependencies TARGETPLATFORM=linux-x64 prefix="usr" DESTDIR="${BUILTDIR}/"

popd > /dev/null || exit 1

# Add native libraries
echo "Downloading dependencies"
if command -v curl >/dev/null 2>&1; then
	curl -s -L -O https://github.com/OpenRA/AppImageSupport/releases/download/${DEPENDENCIES_TAG}/mono.tar.bz2 || exit 3
	curl -s -L -O https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
else
	wget -cq https://github.com/OpenRA/AppImageSupport/releases/download/${DEPENDENCIES_TAG}/mono.tar.bz2 || exit 3
	wget -cq https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
fi

chmod a+x appimagetool-x86_64.AppImage

echo "Building AppImage"
mkdir libs
pushd libs
tar xf ../mono.tar.bz2

install -d "${BUILTDIR}/usr/bin"
install -d "${BUILTDIR}/etc/mono/4.5"
install -d "${BUILTDIR}/usr/lib/mono/4.5/Facades"

install -Dm 0755 usr/bin/mono "${BUILTDIR}/usr/bin/"

install -Dm 0644 /etc/mono/config "${BUILTDIR}/etc/mono/"
install -Dm 0644 /etc/mono/4.5/machine.config "${BUILTDIR}/etc/mono/4.5"

for f in $(ls usr/lib/mono/4.5/Facades/*.dll); do install -Dm 0644 "$f" "${BUILTDIR}/usr/lib/mono/4.5/Facades/"; done
for f in $(ls usr/lib/mono/4.5/*.dll usr/lib/mono/4.5/*.exe); do install -Dm 0644 "$f" "${BUILTDIR}/usr/lib/mono/4.5/"; done
for f in $(ls usr/lib/*.so); do install -Dm 0755 "$f" "${BUILTDIR}/usr/lib/"; done

popd

rm -rf libs mono.tar.bz2

build_appimage() {
	MOD_ID=${1}
	DISPLAY_NAME=${2}
	APPDIR="$(pwd)/${MOD_ID}.appdir"
	APPIMAGE="OpenRA-$(echo "${DISPLAY_NAME}" | sed 's/ /-/g')${SUFFIX}-x86_64.AppImage"

	cp -r "${BUILTDIR}" "${APPDIR}"

	# Add mod files
	pushd "${SRCDIR}" > /dev/null || exit 1
	cp -r "mods/${MOD_ID}" mods/modcontent "${APPDIR}/usr/lib/openra/mods"
	popd > /dev/null || exit 1

	# Add launcher and icons
	sed "s/{MODID}/${MOD_ID}/g" AppRun.in | sed "s/{MODNAME}/${DISPLAY_NAME}/g" > AppRun.temp
	install -m 0755 AppRun.temp "${APPDIR}/AppRun"

	sed "s/{MODID}/${MOD_ID}/g" openra.desktop.in | sed "s/{MODNAME}/${DISPLAY_NAME}/g" | sed "s/{TAG}/${TAG}/g" > temp.desktop
	echo "StartupWMClass=openra-${MOD_ID}-${TAG}" >> temp.desktop

	install -Dm 0755 temp.desktop "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
	install -m 0755 temp.desktop "${APPDIR}/openra-${MOD_ID}.desktop"

	sed "s/{MODID}/${MOD_ID}/g" openra-mimeinfo.xml.in | sed "s/{TAG}/${TAG}/g" > temp.xml
	install -Dm 0755 temp.xml "${APPDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"

	if [ -f "icons/${MOD_ID}_scalable.svg" ]; then
		install -Dm644 "icons/${MOD_ID}_scalable.svg" "${APPDIR}/usr/share/icons/hicolor/scalable/apps/openra-${MOD_ID}.svg"
	fi

	for i in 16x16 32x32 48x48 64x64 128x128 256x256 512x512 1024x1024; do
		if [ -f "icons/${MOD_ID}_${i}.png" ]; then
			install -Dm644 "icons/${MOD_ID}_${i}.png" "${APPDIR}/usr/share/icons/hicolor/${i}/apps/openra-${MOD_ID}.png"
			install -m644 "icons/${MOD_ID}_${i}.png" "${APPDIR}/openra-${MOD_ID}.png"
		fi
	done

	sed "s/{MODID}/${MOD_ID}/g" openra.appimage.in | sed "s/{TAG}/${TAG}/g" | sed "s/{MODNAME}/${DISPLAY_NAME}/g" > openra-mod.temp
	install -m 0755 openra-mod.temp "${APPDIR}/usr/bin/openra-${MOD_ID}"

	sed "s/{MODID}/${MOD_ID}/g" openra-server.appimage.in > openra-mod-server.temp
	install -m 0755 openra-mod-server.temp "${APPDIR}/usr/bin/openra-${MOD_ID}-server"

	sed "s/{MODID}/${MOD_ID}/g" openra-utility.appimage.in > openra-mod-utility.temp
	install -m 0755 openra-mod-utility.temp "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"

	install -m 0755 gtk-dialog.py "${APPDIR}/usr/bin/gtk-dialog.py"

	install -m 0755 restore-environment.sh "${APPDIR}/usr/bin/restore-environment.sh"

	# travis-ci doesn't support mounting FUSE filesystems so extract and run the contents manually
	./appimagetool-x86_64.AppImage --appimage-extract

	# Embed update metadata if (and only if) compiled on travis
	if [ ! -z "${TRAVIS_REPO_SLUG}" ]; then
		ARCH=x86_64 ./squashfs-root/AppRun --no-appstream -u "zsync|https://master.openra.net/appimagecheck?mod=${MOD_ID}&channel=${UPDATE_CHANNEL}" "${APPDIR}" "${OUTPUTDIR}/${APPIMAGE}"
		zsyncmake -u "https://github.com/${TRAVIS_REPO_SLUG}/releases/download/${TAG}/${APPIMAGE}" -o "${OUTPUTDIR}/${APPIMAGE}.zsync" "${OUTPUTDIR}/${APPIMAGE}"
	else
		ARCH=x86_64 ./squashfs-root/AppRun --no-appstream "${APPDIR}" "${OUTPUTDIR}/${APPIMAGE}"
	fi

	rm -rf "${APPDIR}"
}

build_appimage "ra" "Red Alert"
build_appimage "cnc" "Tiberian Dawn"
build_appimage "d2k" "Dune 2000"

# Clean up
rm -rf openra-mod.temp openra-mod-server.temp openra-mod-utility.temp temp.desktop temp.xml AppRun.temp appimagetool-x86_64.AppImage squashfs-root "${BUILTDIR}"
