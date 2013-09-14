#!/bin/bash
E_BADARGS=85
if [ $# -ne "4" ]
then
    echo "Usage: `basename $0` version root-dir packaging-dir outputdir"
    exit $E_BADARGS
fi

# Replace any dashes in the version string with periods
PKGVERSION=`echo $1 | sed "s/-/\\./g"`

sed -i "s/{VERSION_FIELD}/$PKGVERSION/" openra.spec
rootdir=`readlink -f $2`
sed -i "s|{ROOT_DIR}|$rootdir|" openra.spec

# List files to avoid owning standard dirs.
for x in `find $rootdir/usr/bin -type f`
do
    y="${x#$rootdir}"
    sed -i "/%files/ a ${y}" openra.spec
done

for x in `find $rootdir/usr/share/icons -type f`
do
    y="${x#$rootdir}"
    sed -i "/%files/ a ${y}" openra.spec
done

for x in `find $rootdir/usr/share/applications -type f`
do
    y="${x#$rootdir}"
    sed -i "/%files/ a ${y}" openra.spec
done

# List directories only to avoid spam and problems with files containing spaces.
for x in `find $rootdir/usr/share/openra -type d`
do
    y="${x#$rootdir}"
    sed -i "/%files/ a ${y}" openra.spec
done

cp openra.spec "$3/SPECS/"

cd "$3"

rpmbuild --target noarch --buildroot /tmp/openra/ -bb SPECS/openra.spec
if [ $? -ne 0 ]; then
  exit 1
fi

cd RPMS/noarch/
mv openra-$PKGVERSION-1.noarch.rpm $4

