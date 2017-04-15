#!/bin/bash

TAG="$1"
BUILTDIR="$2"
SRCDIR="$3"
OUTPUTDIR="$4"

LAUNCHER_LIBS="-r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:${BUILTDIR}/OpenRA.Game.exe"
FAQ_URL="http://wiki.openra.net/FAQ"

SUFFIX=" (dev)"
if [[ $TAG == release* ]]; then
	SUFFIX=""
elif [[ $TAG == playtest* ]]; then
	SUFFIX=" (playtest)"
fi

function makelauncher()
{
	sed "s|DISPLAY_NAME|$2|" WindowsLauncher.cs.in | sed "s|MOD_ID|$3|" | sed "s|FAQ_URL|${FAQ_URL}|" > WindowsLauncher.cs
	mcs -sdk:4.5 WindowsLauncher.cs -warn:4 -codepage:utf8 -warnaserror -out:"$1" -t:winexe ${LAUNCHER_LIBS} -win32icon:"$4"
	rm WindowsLauncher.cs
	mono ${SRCDIR}/fixheader.exe $1 > /dev/null
}

echo "Compiling Windows launchers"
makelauncher ${BUILTDIR}/RedAlert.exe "Red Alert" "ra" RedAlert.ico
makelauncher ${BUILTDIR}/TiberianDawn.exe "Tiberian Dawn" "cnc" TiberianDawn.ico
makelauncher ${BUILTDIR}/Dune2000.exe "Dune 2000" "d2k" Dune2000.ico

# Windows specific icons
cp OpenRA.ico RedAlert.ico TiberianDawn.ico Dune2000.ico ${BUILTDIR}

if [ -x /usr/bin/makensis ]; then
    echo "Building Windows setup.exe"
    makensis -V2 -DSRCDIR="$BUILTDIR" -DDEPSDIR="${SRCDIR}/thirdparty/download/windows" -DTAG="${TAG}" -DSUFFIX="${SUFFIX}" OpenRA.nsi
    if [ $? -eq 0 ]; then
        mv OpenRA.Setup.exe "$OUTPUTDIR"/OpenRA-$TAG.exe
    else
        exit 1
    fi
else
    echo "Skipping Windows setup.exe build due to missing NSIS"
fi

# Cleanup
rm ${BUILTDIR}/OpenRA.ico ${BUILTDIR}/RedAlert.ico ${BUILTDIR}/TiberianDawn.ico ${BUILTDIR}/Dune2000.ico
rm ${BUILTDIR}/RedAlert.exe ${BUILTDIR}/TiberianDawn.exe ${BUILTDIR}/Dune2000.exe
