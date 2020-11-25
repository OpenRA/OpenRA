#!/bin/bash

command -v curl >/dev/null 2>&1 || { echo >&2 "Windows packaging requires curl."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }
command -v convert >/dev/null 2>&1 || { echo >&2 "Windows packaging requires ImageMagick."; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo >&2 "Windows packaging requires python 3."; exit 1; }

if [ $# -ne "2" ]; then
	echo "Usage: $(basename "$0") tag outputdir"
	exit 1
fi

# Set the working dir to the location of this script
cd "$(dirname "$0")" || exit 1

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"
ARTWORK_DIR="$(pwd)/../artwork/"

LAUNCHER_LIBS="-r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:${BUILTDIR}/OpenRA.Game.dll"
FAQ_URL="http://wiki.openra.net/FAQ"

SUFFIX=" (dev)"
if [[ ${TAG} == release* ]]; then
	SUFFIX=""
elif [[ ${TAG} == playtest* ]]; then
	SUFFIX=" (playtest)"
fi

function makelauncher()
{
	LAUNCHER_NAME="${1}"
	DISPLAY_NAME="${2}"
	MOD_ID="${3}"
	PLATFORM="${4}"

	# Create multi-resolution icon
	convert "${ARTWORK_DIR}/${MOD_ID}_16x16.png" "${ARTWORK_DIR}/${MOD_ID}_24x24.png" "${ARTWORK_DIR}/${MOD_ID}_32x32.png" "${ARTWORK_DIR}/${MOD_ID}_48x48.png" "${ARTWORK_DIR}/${MOD_ID}_256x256.png" "${BUILTDIR}/${MOD_ID}.ico"

	# Create mod-specific launcher
	msbuild -t:Build "${SRCDIR}/OpenRA.WindowsLauncher/OpenRA.WindowsLauncher.csproj" -restore -p:Configuration=Release -p:TargetPlatform="${PLATFORM}" -p:LauncherName="${LAUNCHER_NAME}" -p:LauncherIcon="${BUILTDIR}/${MOD_ID}.ico" -p:ModID="${MOD_ID}" -p:DisplayName="${DISPLAY_NAME}" -p:FaqUrl="${FAQ_URL}"
	cp "${SRCDIR}/bin/${LAUNCHER_NAME}.exe" "${BUILTDIR}"
	cp "${SRCDIR}/bin/${LAUNCHER_NAME}.exe.config" "${BUILTDIR}"

	# Enable the full 4GB address space for the 32 bit game executable
	# The server and utility do not use enough memory to need this
	if [ "${PLATFORM}" = "win-x86" ]; then
		python3 MakeLAA.py "${BUILTDIR}/${LAUNCHER_NAME}.exe"
	fi
}

function build_platform()
{
	PLATFORM="${1}"

	echo "Building core files (${PLATFORM})"
	if [ "${PLATFORM}" = "win-x86" ]; then
		USE_PROGRAMFILES32="-DUSE_PROGRAMFILES32=true"
	else
		USE_PROGRAMFILES32=""
	fi

	pushd "${SRCDIR}" > /dev/null || exit 1
	make clean
	make core TARGETPLATFORM="${PLATFORM}"
	make version VERSION="${TAG}"
	make install-engine TARGETPLATFORM="${PLATFORM}" gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-common-mod-files gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-default-mods gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-dependencies TARGETPLATFORM="${PLATFORM}" gameinstalldir="" DESTDIR="${BUILTDIR}"
	popd > /dev/null || exit 1

	echo "Compiling Windows launchers (${PLATFORM})"
	makelauncher "RedAlert" "Red Alert" "ra" ${PLATFORM}
	makelauncher "TiberianDawn" "Tiberian Dawn" "cnc" ${PLATFORM}
	makelauncher "Dune2000" "Dune 2000" "d2k" ${PLATFORM}

	# Remove redundant generic launcher
	rm "${BUILTDIR}/OpenRA.exe"

	echo "Building Windows setup.exe ($1)"
	makensis -V2 -DSRCDIR="${BUILTDIR}" -DTAG="${TAG}" -DSUFFIX="${SUFFIX}" ${USE_PROGRAMFILES32} OpenRA.nsi
	if [ $? -eq 0 ]; then
		mv OpenRA.Setup.exe "${OUTPUTDIR}/OpenRA-$TAG-$1.exe"
	else
		exit 1
	fi

	echo "Packaging zip archive ($1)"
	pushd "${BUILTDIR}" > /dev/null
	zip "OpenRA-${TAG}-${1}-winportable.zip" -r -9 * --quiet
	mv "OpenRA-${TAG}-${1}-winportable.zip" "${OUTPUTDIR}"
	popd > /dev/null

	rm -rf "${BUILTDIR}"
}

build_platform "win-x86"
build_platform "win-x64"
