#!/bin/bash
# OpenRA packaging master script for linux packages

if [ $# -ne "4" ]; then
	echo "Usage: `basename $0` tag files-dir platform-files-dir outputdir"
    exit 1
fi

TAG=$1
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`
BUILTDIR=$2
DEPSDIR=$3
PACKAGEDIR=$4
ROOTDIR=root

# Clean up
rm -rf $ROOTDIR

cd ../..

# Copy files for OpenRA.Game.exe and OpenRA.Editor.exe as well as all dependencies.
make install-all prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"

# Native library dependencies
cp "$DEPSDIR"/* "$PWD/packaging/linux/$ROOTDIR/usr/lib/openra/" || exit 3

# Icons and .desktop files
make install-shortcuts prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"

# Documentation
mkdir -p $PWD/packaging/linux/$ROOTDIR/usr/share/doc/openra/
cp *.html $PWD/packaging/linux/$ROOTDIR/usr/share/doc/openra/

cd packaging/linux

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

