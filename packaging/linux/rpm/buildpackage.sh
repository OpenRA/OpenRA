#!/bin/bash
E_BADARGS=85
if [ $# -ne "3" ]
then
    echo "Usage: `basename $0` version packaging-dir outputdir"
    exit $E_BADARGS
fi

PKGVERSION=`echo $1 | sed "s/-/\\./g"`
sed -i "s/%define version [0-9\\.]\+/%define version $PKGVERSION/" openra.spec
cp openra.spec "$2/SPECS/"

cd "$2"

rpmbuild --target noarch -bb SPECS/openra.spec
if [ $? -ne 0 ]; then
  exit 1
fi

cd RPMS/noarch/
mv openra-$PKGVERSION-1.noarch.rpm $3
