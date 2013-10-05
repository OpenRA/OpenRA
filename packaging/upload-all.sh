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
upload source ${TAG}.tar.gz
