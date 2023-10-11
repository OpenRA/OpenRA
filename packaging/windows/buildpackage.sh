#!/bin/bash
# OpenRA packaging script for Windows

set -o errexit -o pipefail || exit $?

command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "Windows packaging requires curl or wget."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }
command -v convert >/dev/null 2>&1 || { echo >&2 "Windows packaging requires ImageMagick."; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo >&2 "Windows packaging requires python 3."; exit 1; }
command -v wine64 >/dev/null 2>&1 || { echo >&2 "Windows packaging requires wine64."; exit 1; }

# Set the working dir to the location of this script
HERE=$(dirname "$0")
cd "${HERE}"
. ../functions.sh

TAG="${1:-devtest-19700101}"

if [ -z ${2:-} ]; then
OUTPUTDIR=$(pwd)/../../build/windows
mkdir -p $OUTPUTDIR
else
OUTPUTDIR="${2}"
fi

SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"
ARTWORK_DIR="$(pwd)/../artwork/"

FAQ_URL="https://wiki.openra.net/FAQ"

SUFFIX=" (dev)"
if [[ ${TAG} == release* ]]; then
	SUFFIX=""
elif [[ ${TAG} == playtest* ]]; then
	SUFFIX=" (playtest)"
fi

if command -v curl >/dev/null 2>&1; then
	curl -s -L -o $HOME/rcedit-x64.exe https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x64.exe
else
	wget -cq -O $HOME/rcedit-x64.exe https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x64.exe
fi

function makelauncher()
{
	LAUNCHER_NAME="${1}"
	DISPLAY_NAME="${2}"
	MOD_ID="${3}"
	PLATFORM="${4}"

	TAG_TYPE="${TAG%%-*}"
	TAG_VERSION="${TAG#*-}"
	BACKWARDS_TAG="${TAG_VERSION}-${TAG_TYPE}"

	convert "${ARTWORK_DIR}/${MOD_ID}_16x16.png" "${ARTWORK_DIR}/${MOD_ID}_24x24.png" "${ARTWORK_DIR}/${MOD_ID}_32x32.png" "${ARTWORK_DIR}/${MOD_ID}_48x48.png" "${ARTWORK_DIR}/${MOD_ID}_256x256.png" "${BUILTDIR}/${MOD_ID}.ico"
	install_windows_launcher "${SRCDIR}" "${BUILTDIR}" "win-${PLATFORM}" "${MOD_ID}" "${LAUNCHER_NAME}" "${DISPLAY_NAME}" "${FAQ_URL}" "${TAG}"

	# Use rcedit to patch the generated EXE with missing assembly/PortableExecutable information because .NET 6 ignores that when building on Linux.
	# Using a backwards version tag because rcedit is unable to set versions starting with a letter.
	wine64 $HOME/rcedit-x64.exe "${BUILTDIR}/${LAUNCHER_NAME}.exe" --set-product-version "${BACKWARDS_TAG}"
	wine64 $HOME/rcedit-x64.exe "${BUILTDIR}/${LAUNCHER_NAME}.exe" --set-version-string "ProductName" "OpenRA"
	wine64 $HOME/rcedit-x64.exe "${BUILTDIR}/${LAUNCHER_NAME}.exe" --set-version-string "CompanyName" "The OpenRA team"
	wine64 $HOME/rcedit-x64.exe "${BUILTDIR}/${LAUNCHER_NAME}.exe" --set-version-string "FileDescription" "${LAUNCHER_NAME} mod for OpenRA"
	wine64 $HOME/rcedit-x64.exe "${BUILTDIR}/${LAUNCHER_NAME}.exe" --set-version-string "LegalCopyright" "Copyright (c) The OpenRA Developers and Contributors"
	wine64 $HOME/rcedit-x64.exe "${BUILTDIR}/${LAUNCHER_NAME}.exe" --set-icon "${BUILTDIR}/${MOD_ID}.ico"
}

function build_platform()
{
	PLATFORM="${1}"

	echo "Building core files (${PLATFORM})"
	if [ "${PLATFORM}" = "x86" ]; then
		USE_PROGRAMFILES32="-DUSE_PROGRAMFILES32=true"
	else
		USE_PROGRAMFILES32=""
	fi

	install_assemblies "${SRCDIR}" "${BUILTDIR}" "win-${PLATFORM}" "net6" "False" "True" "True"
	install_data "${SRCDIR}" "${BUILTDIR}" "cnc" "d2k" "ra"
	set_engine_version "${TAG}" "${BUILTDIR}"
	set_mod_version "${TAG}" "${BUILTDIR}/mods/cnc/mod.yaml" "${BUILTDIR}/mods/d2k/mod.yaml" "${BUILTDIR}/mods/ra/mod.yaml"  "${BUILTDIR}/mods/modcontent/mod.yaml"

	echo "Compiling Windows launchers (${PLATFORM})"
	makelauncher "RedAlert" "Red Alert" "ra" "${PLATFORM}"
	makelauncher "TiberianDawn" "Tiberian Dawn" "cnc" "${PLATFORM}"
	makelauncher "Dune2000" "Dune 2000" "d2k" "${PLATFORM}"

	echo "Building Windows setup.exe ($1)"
	makensis -V2 -DSRCDIR="${BUILTDIR}" -DTAG="${TAG}" -DSUFFIX="${SUFFIX}" -DOUTFILE="${OUTPUTDIR}/OpenRA-${TAG}-${PLATFORM}.exe" ${USE_PROGRAMFILES32} OpenRA.nsi

	echo "Packaging zip archive ($1)"
	pushd "${BUILTDIR}" > /dev/null
	zip "OpenRA-${TAG}-${PLATFORM}-winportable.zip" -r -9 ./* --quiet
	mv "OpenRA-${TAG}-${PLATFORM}-winportable.zip" "${OUTPUTDIR}"
	popd > /dev/null

	rm -rf "${BUILTDIR}"
}

build_platform "x86"
build_platform "x64"
rm $HOME/rcedit-x64.exe
