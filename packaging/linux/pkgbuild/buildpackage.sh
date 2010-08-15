#!/bin/bash
ARGS=5
E_BADARGS=85

if [ $# -ne "$ARGS" ]
then
    echo "Usage: `basename $0` ftp-server ftp-path username password version"
    exit $E_BADARGS
fi

PKGVERSION=`echo $5 | sed "s/-/\\./g"`
sed -i "s/pkgver=[0-9\\.]\+/pkgver=$PKGVERSION/" PKGBUILD

makepkg --holdver
if [ $? -ne 0 ]; then
  exit 1
fi

PACKAGEFILE="openra-git-$PKGVERSION-1-any.pkg.tar.xz"

size=`stat -c "%s" $PACKAGEFILE`

echo "$5,$size,$PACKAGEFILE" > archlatest.txt

wput $PACKAGEFILE "ftp://$3:$4@$1/$2/"
wput -u archlatest.txt "ftp://$3:$4@$1/$2/"

