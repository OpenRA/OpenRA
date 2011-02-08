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
TYPE=`echo ${TAG} | grep -o "^[a-z]\\+"`
VERSION=`echo ${TAG} | grep -o "[0-9]\\+-\\?[0-9]\\?"`
LINUXVERSION=`echo ${TAG} | sed "s/-/\\./g"`

cd ${PKGDIR}
upload windows OpenRA-${TAG}.exe
upload mac OpenRA-${TAG}.zip
upload linux/deb openra_${LINUXVERSION}_all.deb

mv openra-${VERSION}-1.noarch.rpm openra-${TYPE}${VERSION}-1.noarch.rpm
upload linux/rpm openra-${TYPE}${VERSION}-1.noarch.rpm
mv openra-${VERSION}-1-any.pkg.tar.xz openra-${TYPE}${VERSION}-1-any.pkg.tar.xz
upload linux/arch openra-${TYPE}${VERSION}-1-any.pkg.tar.xz

wget http://${FTPSERVER}/home/syncdownloads
