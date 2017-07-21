#!/bin/bash

command -v curl >/dev/null 2>&1 || { echo >&2 "Windows packaging requires curl."; exit 1; }
command -v markdown >/dev/null 2>&1 || { echo >&2 "Windows packaging requires markdown."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }

if [ $# -ne "2" ]; then
	echo "Usage: `basename $0` tag outputdir"
    exit 1
fi

# Set the working dir to the location of this script
cd $(dirname $0)

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"

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
	sed "s|DISPLAY_NAME|$2|" WindowsLauncher.cs.in | sed "s|MOD_ID|$3|" | sed "s|FAQ_URL|${FAQ_URL}|" > WindowsLauncher.cs
	mcs -sdk:4.5 WindowsLauncher.cs -warn:4 -codepage:utf8 -warnaserror -out:"$1" -t:winexe ${LAUNCHER_LIBS} -win32icon:"$4"
	rm WindowsLauncher.cs
	mono "${SRCDIR}/fixheader.exe" $1 > /dev/null
}

echo "Building core files"

pushd ${SRCDIR} > /dev/null
make windows-dependencies
make core SDK="-sdk:4.5"
make version VERSION="${TAG}"
make install-core gameinstalldir="" DESTDIR="${BUILTDIR}"
popd > /dev/null

echo "Compiling Windows launchers"
makelauncher "${BUILTDIR}/RedAlert.exe" "Red Alert" "ra" RedAlert.ico
makelauncher "${BUILTDIR}/TiberianDawn.exe" "Tiberian Dawn" "cnc" TiberianDawn.ico
makelauncher "${BUILTDIR}/Dune2000.exe" "Dune 2000" "d2k" Dune2000.ico

# Windows specific files
cp OpenRA.ico RedAlert.ico TiberianDawn.ico Dune2000.ico "${BUILTDIR}"
cp "${SRCDIR}/OpenRA.Game.exe.config" "${BUILTDIR}"

curl -s -L -O https://raw.githubusercontent.com/wiki/OpenRA/OpenRA/Changelog.md
markdown Changelog.md > "${BUILTDIR}/CHANGELOG.html"
rm Changelog.md

markdown "${SRCDIR}/README.md" > "${BUILTDIR}/README.html"
markdown "${SRCDIR}/CONTRIBUTING.md" > "${BUILTDIR}/CONTRIBUTING.html"

echo "Building Windows setup.exe"
makensis -V2 -DSRCDIR="${BUILTDIR}" -DDEPSDIR="${SRCDIR}/thirdparty/download/windows" -DTAG="${TAG}" -DSUFFIX="${SUFFIX}" OpenRA.nsi
if [ $? -eq 0 ]; then
    mv OpenRA.Setup.exe "${OUTPUTDIR}/OpenRA-$TAG.exe"
else
    exit 1
fi

# Cleanup
rm -rf "${BUILTDIR}"
