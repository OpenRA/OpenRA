#!/bin/bash
ARGS=6
E_BADARGS=85

if [ $# -ne "$ARGS" ]
then
    echo "Usage: `basename $0` ftp-server ftp-path username password version packaging-dir"
    exit $E_BADARGS
fi

PKGVERSION=`echo $5 | sed "s/-/\\./g"`
sed -i "s/%define version [0-9\\.]\+/%define version $PKGVERSION/" openra.spec
cp openra.spec $6/SPECS/

cd $6
wget http://github.com/chrisforbes/OpenRA/tarball/playtest-$5 -O SOURCES/openra-$PKGVERSION.tar.gz

folder=`tar -ztf SOURCES/openra-$PKGVERSION.tar.gz | head -n 1 | grep -o -E [[:alnum:]-]+`

sed -i "s/%define folder [[:alnum:]-]\+/%define folder $folder/" SPECS/openra.spec

rpmbuild --target noarch -bb SPECS/openra.spec

cd RPMS/noarch/
PACKAGEFILE=openra-$PKGVERSION-1.noarch.rpm
size=`stat -c "%s" $PACKAGEFILE`

echo "$5,$size,$PACKAGEFILE" > /tmp/rpmlatest.txt

wput $PACKAGEFILE "ftp://$3:$4@$1/$2/"
cd /tmp
wput -u rpmlatest.txt "ftp://$3:$4@$1/$2/"
