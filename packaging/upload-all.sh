#!/bin/bash
FTPSERVER=openra.res0l.net
TAG=$1

TYPE=`echo $TAG | grep -o "^[a-z]\\+"`
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`

case "$TYPE" in
    "release") 
        FTPPATH="openra.res0l.net/releases"
        ;;
    "playtest") 
        FTPPATH="openra.res0l.net/playtests"
        ;;
    *)
        msg "\E[31m" "Unrecognized tag prefix $TYPE"
        exit 1
        ;;
esac

uploader.sh windows "$VERSION" OpenRA-$VERSION.exe "$FTPPATH" "$2" "$3"
uploader.sh mac "$VERSION" OpenRA-$VERSION.zip "$FTPPATH" "$2" "$3"

if [ "$TYPE" = "release" ]; then
    wput --basename=../ -u ../VERSION ftp://$2:$3@$FTPSERVER/$FTPPATH/master/
fi