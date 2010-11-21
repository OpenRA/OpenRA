#!/bin/bash
FTPSERVER=openra.res0l.net
TAG=$1
PKGDIR=$2

TYPE=`echo $TAG | grep -o "^[a-z]\\+"`
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`
FTPPATHBASE="openra.res0l.net"

case "$TYPE" in
    "release") 
        FTPPATH="$FTPPATHBASE/releases"
        ;;
    "playtest") 
        FTPPATH="$FTPPATHBASE/playtests"
        ;;
    *)
        msg "\E[31m" "Unrecognized tag prefix $TYPE"
        exit 1
        ;;
esac

uploader.sh windows "$VERSION" $PKGDIR/OpenRA-$VERSION.exe "latest" "$FTPPATH" "$3" "$4"
uploader.sh mac "$VERSION" $PKGDIR/OpenRA-$VERSION.zip "latest" "$FTPPATH" "$3" "$4"

LINUXVERSION=`echo $VERSION | sed "s/-/\\./g"`

uploader.sh linux "$VERSION" $PKGDIR/openra-$LINUXVERSION.deb "deblatest" "$FTPPATH" "$3" "$4"
uploader.sh linux "$VERSION" $PKGDIR/openra-$LINUXVERSION-1.noarch.rpm "rpmlatest" "$FTPPATH" "$3" "$4"
uploader.sh linux "$VERSION" $PKGDIR/openra-$LINUXVERSION-1-any.pkg.tar.xz "archlatest" "$FTPPATH" "$3" "$4"

if [ "$TYPE" = "release" ]; then
    wput --basename=../ -u ../VERSION ftp://$3:$4@$FTPSERVER/$FTPPATHBASE/master/
    cp ../VERSION ../srclatest.txt
    wput --basename=../ -u ../srclatest.txt ftp://$3:$4@$FTPSERVER/$FTPPATH/linux/
fi