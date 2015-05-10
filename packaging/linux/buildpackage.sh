#!/bin/bash
# OpenRA packaging master script for linux packages

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` tag files-dir outputdir"
    exit 1
fi

TAG=$1
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`
BUILTDIR=$2
PACKAGEDIR=$3
ROOTDIR=root

# Clean up
rm -rf $ROOTDIR

cd ../..

# Copy files for OpenRA.Game.exe and OpenRA.Editor.exe as well as all dependencies.
make install-all prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"

# Install startup scripts, desktop files and icons
make install-linux-shortcuts prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"
make install-linux-mime prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"
make install-linux-appdata prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"

# Documentation
mkdir -p $PWD/packaging/linux/$ROOTDIR/usr/share/doc/openra/
cp *.html $PWD/packaging/linux/$ROOTDIR/usr/share/doc/openra/

cd packaging/linux

pushd deb
echo "Building Debian package."
bash buildpackage.sh "$TAG" ../$ROOTDIR "$PACKAGEDIR"
if [ $? -ne 0 ]; then
    echo "Debian package build failed."
fi
popd
