#!/bin/bash
# OpenRA packaging master script for linux packages

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` version files-dir outputdir"
    exit 1
fi

TAG=$1
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`
BUILTDIR=$2
PACKAGEDIR=$3
ROOTDIR=root

# Clean up
rm -rf $ROOTDIR

# Game files
mkdir -p $ROOTDIR/usr/bin/
cp -T openra-bin $ROOTDIR/usr/bin/openra
mkdir -p $ROOTDIR/usr/share/openra/
cp -R $BUILTDIR/* "$ROOTDIR/usr/share/openra/" || exit 3

# Desura launch scripts
cp ../../*.sh "$ROOTDIR/usr/share/openra/" || exit 3

# Desktop Icons
mkdir -p $ROOTDIR/usr/share/applications/
cp openra.desktop "$ROOTDIR/usr/share/applications/"

mkdir -p $ROOTDIR/usr/share/icons/
cp -r hicolor $ROOTDIR/usr/share/icons/

(
    echo "Building Debian package."
    cd deb
    ./buildpackage.sh "$TAG" ../$ROOTDIR "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "Debian package build failed, refer to $PWD/package.log."
    fi
) &

(
    echo "Building Arch-Linux package."
    cd pkgbuild
    sh buildpackage.sh "$TAG" ../$ROOTDIR "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "Arch-Linux package build failed, refer to $PWD/package.log."
    fi
) &
     
(
    echo "Building RPM package."
    cd rpm
    sh buildpackage.sh "$TAG" ../$ROOTDIR ~/rpmbuild "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "RPM package build failed, refer to $PWD/package.log."
    fi
) &
 
wait

