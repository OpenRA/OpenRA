#!/bin/bash

set -x

TEMPLATE_DMG=/tmp/ZeroBraneStudio-template.dmg
BUILT_DMG=ZeroBraneStudio.dmg
WKDIR=/tmp/zbs-build

# remove problematic symlink
rm ../zbstudio/ZeroBraneStudio.app/Contents/ZeroBraneStudio

bunzip2 -kf ZeroBraneStudio.dmg.bz2
mv ZeroBraneStudio.dmg $TEMPLATE_DMG
hdiutil attach "${TEMPLATE_DMG}" -noautoopen -quiet -mountpoint "${WKDIR}"

rm -rf "${WKDIR}/ZeroBraneStudio.app"

# copy the app to where it should be
cp -pr "../zbstudio/ZeroBraneStudio.app" "${WKDIR}/ZeroBraneStudio.app"

mkdir "${WKDIR}/ZeroBraneStudio.app/Contents/ZeroBraneStudio"

# only pick the files listed in manifests and 'myprograms' (if exists)
if [[ -d ../myprograms ]]; then MYPROGRAMS=$(cd ..; find myprograms -iname *.lua); fi
(cd ".."; tar cf - $MYPROGRAMS $(< zbstudio/MANIFEST) $(< zbstudio/MANIFEST-bin-macos) | (cd "${WKDIR}/ZeroBraneStudio.app/Contents/ZeroBraneStudio/"; tar xf -))

codesign -s "ZeroBrane LLC" ${WKDIR}/ZeroBraneStudio.app
codesign --signature-size 6400 -s "ZeroBrane LLC" ${WKDIR}/ZeroBraneStudio.app/Contents/ZeroBraneStudio/bin/lua.app

# clean up
sudo rm -rf "${WKDIR}/.Trashes"
sudo rm -rf "${WKDIR}/.fseventsd"

hdiutil detach "${WKDIR}" -quiet -force
hdiutil convert "${TEMPLATE_DMG}" -quiet -format UDZO -imagekey zlib-level=9 -o "${BUILT_DMG}"

rm -f "${TEMPLATE_DMG}"

cd ../zbstudio/ZeroBraneStudio.app/Contents
ln -s ../../.. ZeroBraneStudio

echo Built ${BUILT_DMG}.
