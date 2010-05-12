#!/bin/bash
ARGS=5
E_BADARGS=85

if [ $# -ne "$ARGS" ]
then
    echo "Usage: `basename $0` ftp-server ftp-path username password version"
    exit $E_BADARGS
fi

sed -i "s/pkgver=[0-9]\+/pkgver=$5/" PKGBUILD

makepkg --holdver

PACKAGEFILE="openra-git-$5-1-any.pkg.tar.xz"

size=`stat -c "%s" $PACKAGEFILE`

echo "$5,$size,$PACKAGEFILE" > archlatest.txt

wput $PACKAGEFILE "ftp://$3:$4@$1/$2/"
wput -u archlatest.txt "ftp://$3:$4@$1/$2/"

