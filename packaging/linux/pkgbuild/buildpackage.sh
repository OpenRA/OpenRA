#!/bin/bash
E_BADARGS=85
if [ $# -ne "3" ]
then
    echo "Usage: `basename $0` version root-dir outputdir"
    exit $E_BADARGS
fi

PKGVERSION=`echo $1 | sed "s/-/\\./g"`
sed -i "s/{VERSION}/$PKGVERSION/" PKGBUILD
sed -i "s/{ROOT}/$2/" PKGBUILD

makepkg --holdver
if [ $? -ne 0 ]; then
  exit 1
fi

mv openra-$PKGVERSION-1-any.pkg.tar.xz $3