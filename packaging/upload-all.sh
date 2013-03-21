#!/bin/bash

UPLOADUSER=openra
SERVER=openra.res0l.net
PATHBASE="openra.res0l.net/assets/downloads"

upload () {
    PLATFORM=$1
    FILENAME=$2
    scp "${FILENAME}" ${UPLOADUSER}@${SERVER}:${PATHBASE}/${PLATFORM}/
}

TAG=$1
PKGDIR=$2
LINUXVERSION=`echo ${TAG} | sed "s/-/\\./g"`

cd ${PKGDIR}
upload windows OpenRA-${TAG}.exe
upload mac OpenRA-${TAG}.zip
upload linux/deb openra_${LINUXVERSION}_all.deb
upload linux/rpm openra-${LINUXVERSION}-1.noarch.rpm
upload linux/arch openra-${LINUXVERSION}-1-any.pkg.tar.xz

curl http://${SERVER}/home/syncdownloads

echo "Updating https://github.com/OpenRA/OpenRA/wiki/Traits"
rm -rf openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git openra-wiki
cp -fr ../DOCUMENTATION.md openra-wiki/Traits.md
cd openra-wiki
git add Traits.md
git commit -m "Update trait documentation"
git push origin master