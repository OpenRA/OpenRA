#!/bin/bash

TAG="$1"
BUILTDIR="$2"
SRCDIR="$3"
OUTPUTDIR="$4"

if [ -x /usr/bin/makensis ]; then
    pushd "$SRCDIR" >/dev/null
    popd >/dev/null
    if [[ ! -f /usr/share/nsis/Include/nsProcess.nsh &&  ! -f /usr/share/nsis/Plugin/nsProcess.dll ]]; then
        echo "Installing NsProcess NSIS plugin in /usr/share/nsis"
        sudo unzip -qq -o ${SRCDIR}/thirdparty/download/NsProcess.zip 'Include/*' -d /usr/share/nsis
        sudo unzip -qq -j -o ${SRCDIR}/thirdparty/download/NsProcess.zip 'Plugin/*' -d /usr/share/nsis/Plugins
    fi
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
