#!/bin/bash
ARGS=5
E_BADARGS=85

if [ $# -ne "$ARGS" ]
then
    echo "Usage: `basename $0` ftp-server ftp-path username password version"
    exit $E_BADARGS
fi

sed -i s/pkgver=[0-9]+/pkgver=$5/ PKGBUILD

makepkg --holdver

PACKAGEFILE="openra-git-$5-1-any.pkg.tar.xz"

size=`stat -c "%s" $PACKAGEFILE`

echo "$5,$size,$PACKAGEFILE" > archlatest.txt

ftp -n -v $1 << cmd
user "$3" "$4"
cd $2
put $PACKAGEFILE
put archlatest.txt
cmd



