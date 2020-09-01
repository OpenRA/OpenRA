#!/bin/bash

command -v curl >/dev/null 2>&1 || { echo >&2 "Windows packaging requires curl."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }
command -v convert >/dev/null 2>&1 || { echo >&2 "Windows packaging requires ImageMagick."; exit 1; }

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

LAUNCHER_LIBS="-r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:${BUILTDIR}/OpenRA.Game.exe"
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

	sed "s|DISPLAY_NAME|${DISPLAY_NAME}|" WindowsLauncher.cs.in | sed "s|MOD_ID|${MOD_ID}|" | sed "s|FAQ_URL|${FAQ_URL}|" > WindowsLauncher.cs
	csc WindowsLauncher.cs -warn:4 -warnaserror -platform:"${PLATFORM}" -out:"${BUILTDIR}/${LAUNCHER_NAME}" -t:winexe ${LAUNCHER_LIBS} -win32icon:"${BUILTDIR}/${MOD_ID}.ico"
	rm WindowsLauncher.cs

	if [ "${PLATFORM}" = "x86" ]; then
		# Enable the full 4GB address space for the 32 bit game executable
		# The server and utility do not use enough memory to need this
		csc MakeLAA.cs -warn:4 -warnaserror -out:"MakeLAA.exe"
		mono "MakeLAA.exe" "${BUILTDIR}/${LAUNCHER_NAME}"
		rm MakeLAA.exe
	fi
}

function build_platform()
{
	PLATFORM="${1}"

	echo "Building core files (${PLATFORM})"
	if [ "${PLATFORM}" = "x86" ]; then
		TARGETPLATFORM="TARGETPLATFORM=win-x86"
		IS_WIN32="WIN32=true"
		USE_PROGRAMFILES32="-DUSE_PROGRAMFILES32=true"
	else
		IS_WIN32="WIN32=false"
		TARGETPLATFORM="TARGETPLATFORM=win-x64"
		USE_PROGRAMFILES32=""
	fi

	pushd "${SRCDIR}" > /dev/null || exit 1
	make clean
	make core "${TARGETPLATFORM}" "${IS_WIN32}"
	make version VERSION="${TAG}"
	make install-engine "${TARGETPLATFORM}" gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-common-mod-files gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-default-mods gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-dependencies "${TARGETPLATFORM}" gameinstalldir="" DESTDIR="${BUILTDIR}"
	popd > /dev/null || exit 1

	echo "Compiling Windows launchers (${PLATFORM})"
	makelauncher "RedAlert.exe" "Red Alert" "ra" ${PLATFORM}
	makelauncher "TiberianDawn.exe" "Tiberian Dawn" "cnc" ${PLATFORM}
	makelauncher "Dune2000.exe" "Dune 2000" "d2k" ${PLATFORM}
	cp "${SRCDIR}/OpenRA.Game.exe.config" "${BUILTDIR}"

	echo "Building Windows setup.exe ($1)"
	makensis -V2 -DSRCDIR="${BUILTDIR}" -DTAG="${TAG}" -DSUFFIX="${SUFFIX}" ${USE_PROGRAMFILES32} OpenRA.nsi
	if [ $? -eq 0 ]; then
		mv OpenRA.Setup.exe "${OUTPUTDIR}/OpenRA-$TAG-$1.exe"
	else
		exit 1
	fi

	rm -rf "${BUILTDIR}"
}

build_platform "x86"
build_platform "x64"
