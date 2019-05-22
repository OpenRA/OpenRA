#!/bin/bash

command -v curl >/dev/null 2>&1 || { echo >&2 "Windows packaging requires curl."; exit 1; }
command -v markdown >/dev/null 2>&1 || { echo >&2 "Windows packaging requires markdown."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }

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
	csc WindowsLauncher.cs -warn:4 -warnaserror -platform:"$5" -out:"$1" -t:winexe ${LAUNCHER_LIBS} -win32icon:"$4"
	rm WindowsLauncher.cs
	mono "${SRCDIR}/OpenRA.PostProcess.exe" "$1" -LAA > /dev/null
}

function build_platform()
{
	echo "Building core files ($1)"
	if [ "$1" = "x86" ]; then
		IS_WIN32="WIN32=true"
	else
		IS_WIN32="WIN32=false"
	fi

	pushd "${SRCDIR}" > /dev/null || exit 1
	make clean
	make windows-dependencies "${IS_WIN32}"
	make core "${IS_WIN32}"
	make version VERSION="${TAG}"
	make install-core gameinstalldir="" DESTDIR="${BUILTDIR}"
	popd > /dev/null || exit 1

	echo "Compiling Windows launchers ($1)"
	makelauncher "${BUILTDIR}/RedAlert.exe" "Red Alert" "ra" RedAlert.ico $1
	makelauncher "${BUILTDIR}/TiberianDawn.exe" "Tiberian Dawn" "cnc" TiberianDawn.ico $1
	makelauncher "${BUILTDIR}/Dune2000.exe" "Dune 2000" "d2k" Dune2000.ico $1

	# Windows specific files
	cp OpenRA.ico RedAlert.ico TiberianDawn.ico Dune2000.ico "${BUILTDIR}"
	cp "${SRCDIR}/OpenRA.Game.exe.config" "${BUILTDIR}"

	curl -s -L -O https://raw.githubusercontent.com/wiki/OpenRA/OpenRA/Changelog.md
	markdown Changelog.md > "${BUILTDIR}/CHANGELOG.html"
	rm Changelog.md

	markdown "${SRCDIR}/README.md" > "${BUILTDIR}/README.html"
	markdown "${SRCDIR}/CONTRIBUTING.md" > "${BUILTDIR}/CONTRIBUTING.html"

	echo "Building Windows setup.exe ($1)"
	makensis -V2 -DSRCDIR="${BUILTDIR}" -DDEPSDIR="${SRCDIR}/thirdparty/download/windows" -DTAG="${TAG}" -DSUFFIX="${SUFFIX}" OpenRA.nsi
	if [ $? -eq 0 ]; then
		mv OpenRA.Setup.exe "${OUTPUTDIR}/OpenRA-$TAG-$1.exe"
	else
		exit 1
	fi

	rm -rf "${BUILTDIR}"
}

build_platform "x86"
build_platform "x64"