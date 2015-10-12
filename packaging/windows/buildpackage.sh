#!/bin/bash

TAG="$1"
BUILTDIR="$2"
SRCDIR="$3"
OUTPUTDIR="$4"

if [ -x /usr/bin/makensis ]; then
    echo "Building Windows setup.exe"
    makensis -V2 -DSRCDIR="$BUILTDIR" -DDEPSDIR="${SRCDIR}/thirdparty/download/windows" OpenRA.nsi
    if [ $? -eq 0 ]; then
        mv OpenRA.Setup.exe "$OUTPUTDIR"/OpenRA-$TAG.exe
    else
        exit 1
    fi
else
    echo "Skipping Windows setup.exe build due to missing NSIS"
fi
