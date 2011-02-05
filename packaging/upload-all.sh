#!/bin/bash
FTPSERVER=openra.res0l.net
TAG=$1
PKGDIR=$2

TYPE=`echo $TAG | grep -o "^[a-z]\\+"`
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`
FTPPATHBASE="openra.res0l.net"

FTPPATH="$FTPPATHBASE/assets/downloads"

mv $PKGDIR/OpenRA-$VERSION.exe $PKGDIR/OpenRA-$TYPE$VERSION.exe
./uploader.sh windows $PKGDIR/OpenRA-$TYPE$VERSION.exe "$FTPSERVER" "$FTPPATH" "$3" "$4"
mv $PKGDIR/OpenRA-$VERSION.zip $PKGDIR/OpenRA-$TYPE$VERSION.zip
./uploader.sh mac $PKGDIR/OpenRA-$TYPE$VERSION.zip "$FTPSERVER" "$FTPPATH" "$3" "$4"

LINUXVERSION=`echo $VERSION | sed "s/-/\\./g"`

mv $PKGDIR/openra-$VERSION.deb $PKGDIR/openra-$TYPE$VERSION.deb
./uploader.sh linux/deb $PKGDIR/openra-$TYPE$VERSION.deb "$FTPSERVER" "$FTPPATH" "$3" "$4"
mv $PKGDIR/openra-$LINUXVERSION-1.noarch.rpm $PKGDIR/openra-$TYPE$VERSION-1.noarch.rpm
./uploader.sh linux/rpm $PKGDIR/openra-$TYPE$VERSION-1.noarch.rpm "$FTPSERVER" "$FTPPATH" "$3" "$4"
mv $PKGDIR/openra-$LINUXVERSION-1-any.pkg.tar.xz $PKGDIR/openra-$TYPE$VERSION-1-any.pkg.tar.xz
./uploader.sh linux/arch $PKGDIR/openra-$TYPE$VERSION-1-any.pkg.tar.xz "$FTPSERVER" "$FTPPATH" "$3" "$4"

if [ "$TYPE" = "release" ]; then
    wput --basename=../ -u ../VERSION ftp://$3:$4@$FTPSERVER/$FTPPATHBASE/master/
fi

wget http://$FTPSERVER/home/syncdownloads
