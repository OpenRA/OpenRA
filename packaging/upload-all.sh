#!/bin/bash
FTPSERVER=openra.res0l.net
TAG=$1
PKGDIR=$2

TYPE=`echo $TAG | grep -o "^[a-z]\\+"`
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`
LINUXVERSION=`echo $TAG | sed "s/-/\\./g"`

FTPPATHBASE="openra.res0l.net"
FTPPATH="$FTPPATHBASE/assets/downloads"

./uploader.sh windows $PKGDIR/OpenRA-$TAG.exe "$FTPSERVER" "$FTPPATH"
./uploader.sh mac $PKGDIR/OpenRA-$TAG.zip "$FTPSERVER" "$FTPPATH"
./uploader.sh linux/deb $PKGDIR/openra_${LINUXVERSION}_all.deb "$FTPSERVER" "$FTPPATH"

mv $PKGDIR/openra-$LINUXVERSION-1.noarch.rpm $PKGDIR/openra-$TYPE$VERSION-1.noarch.rpm
./uploader.sh linux/rpm $PKGDIR/openra-$TYPE$VERSION-1.noarch.rpm "$FTPSERVER" "$FTPPATH"
mv $PKGDIR/openra-$LINUXVERSION-1-any.pkg.tar.xz $PKGDIR/openra-$TYPE$VERSION-1-any.pkg.tar.xz
./uploader.sh linux/arch $PKGDIR/openra-$TYPE$VERSION-1-any.pkg.tar.xz "$FTPSERVER" "$FTPPATH"

if [ "$TYPE" = "release" ]; then
    wput --basename=../ -u ../VERSION ftp://$FTPSERVER/$FTPPATHBASE/master/
fi

wget http://$FTPSERVER/home/syncdownloads
