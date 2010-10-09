#!/bin/bash
E_BADARGS=85
if [ $# -ne "2" ]
then
    echo "Usage: `basename $0` version outputdir"
    exit $E_BADARGS
fi

PKGVERSION=`echo $1 | sed "s/-/\\./g"`
sed -i "s/{VERSION}/$PKGVERSION/" PKGBUILD

makepkg --holdver
if [ $? -ne 0 ]; then
  exit 1
fi

mv openra-$PKGVERSION-1-any.pkg.tar.xz $2