#!/bin/bash
E_BADARGS=85
if [ $# -ne "5" ]
then
    echo "Usage: `basename $0` version root-dir packaging-dir outputdir arch"
    exit $E_BADARGS
fi

if [ $5 = "x64" ]
then
	ARCH=x86_64
else
	ARCH=i386
fi

# Replace any dashes in the version string with periods
PKGVERSION=`echo $1 | sed "s/-/\\./g"`

cp openra.spec openra$ARCH.spec

sed -i "s/{VERSION_FIELD}/$PKGVERSION/" openra$ARCH.spec
rootdir=`readlink -f $2`
sed -i "s|{ROOT_DIR}|$rootdir|" openra$ARCH.spec
sed -i "s/{ARCH}/$ARCH/" openra$ARCH.spec

for x in `find $rootdir -type f`
do
    y="${x#$rootdir}"
    sed -i "/%files/ a $y" openra$ARCH.spec
done

cp openra$ARCH.spec "$3/SPECS/"

cd "$3"

rpmbuild --target $ARCH -bb SPECS/openra$ARCH.spec
if [ $? -ne 0 ]; then
  exit 1
fi

cd RPMS/$ARCH/
mv openra-$PKGVERSION-1.$ARCH.rpm $4

