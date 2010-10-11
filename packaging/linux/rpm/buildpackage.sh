#!/bin/bash
E_BADARGS=85
if [ $# -ne "4" ]
then
    echo "Usage: `basename $0` version root-dir packaging-dir outputdir"
    exit $E_BADARGS
fi

PKGVERSION=`echo $1 | sed "s/-/\\./g"`
sed -i "s/{VERSION_FIELD}/$PKGVERSION/" openra.spec
rootdir=`readlink -f $2`
sed -i "s|{ROOT_DIR}|$rootdir|" openra.spec
cp openra.spec "$3/SPECS/"

cd "$3"

rpmbuild --target noarch -bb SPECS/openra.spec
if [ $? -ne 0 ]; then
  exit 1
fi

cd RPMS/noarch/
mv openra-$PKGVERSION-1.noarch.rpm $4

