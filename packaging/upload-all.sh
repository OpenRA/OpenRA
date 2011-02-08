#!/bin/bash
FTPSERVER=openra.res0l.net
FTPPATHBASE="openra.res0l.net"
FTP="ftp://${FTPSERVER}/${FTPPATHBASE}/assets/downloads/"

upload () {
    PLATFORM=$1
    FILENAME=$2
    wput -u "${FTP}${PLATFORM}/" "${FILENAME}"
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

wget http://${FTPSERVER}/home/syncdownloads
