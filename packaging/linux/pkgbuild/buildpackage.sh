#!/bin/bash
E_BADARGS=85
if [ $# -ne "4" ]
then
    echo "Usage: `basename $0` version root-dir outputdir arch"
    exit $E_BADARGS
fi

if [ $4 -eq "x64" ]
then
	ARCH=x86_64
else
	ARCH=i686

# Replace any dashes in the version string with periods
PKGVERSION=`echo $1 | sed "s/-/\\./g"`

sed -i "s/{VERSION}/$PKGVERSION/" PKGBUILD
rootdir=`readlink -f $2`
sed -i "s|{ROOT}|$rootdir|" PKGBUILD
sed -i "s/{ARCH}/$ARCH/" PKGBUILD

makepkg --holdver
if [ $? -ne 0 ]; then
  exit 1
fi

mv openra-$PKGVERSION-1-$ARCH.pkg.tar.xz $3

