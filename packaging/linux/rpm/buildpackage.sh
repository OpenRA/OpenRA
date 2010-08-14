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
cp openra.spec "$6/SPECS/"

pushd "$6"

rpmbuild --target noarch -bb SPECS/openra.spec

pushd RPMS/noarch/
PACKAGEFILE=openra-$PKGVERSION-1.noarch.rpm
size=`stat -c "%s" $PACKAGEFILE`
popd

echo "$5,$size,$PACKAGEFILE" > /tmp/rpmlatest.txt

wput $PACKAGEFILE "ftp://$3:$4@$1/$2/"
pushd /tmp
wput -u rpmlatest.txt "ftp://$3:$4@$1/$2/"
popd
popd
