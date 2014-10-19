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

cp $DEPSDIR/* $PWD/packaging/linux/$ROOTDIR/usr/lib/openra/

# Install startup scripts, desktop files and icons
make install-linux-shortcuts prefix="/usr" DESTDIR="$PWD/packaging/linux/$ROOTDIR"

# Remove the windows launcher
rm $PWD/packaging/linux/$ROOTDIR/usr/lib/openra/OpenRA.exe

# Remove the WinForms tileset builder which does not work outside the source tree
rm $PWD/packaging/linux/$ROOTDIR/usr/lib/openra/OpenRA.TilesetBuilder.exe

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

pushd pkgbuild
echo "Building Arch-Linux package."
bash buildpackage.sh "$TAG" ../$ROOTDIR "$PACKAGEDIR"
if [ $? -ne 0 ]; then
    echo "Arch-Linux package build failed."
fi
popd

pushd rpm
echo "Building RPM package."
bash buildpackage.sh "$TAG" ../$ROOTDIR ~/rpmbuild "$PACKAGEDIR"
if [ $? -ne 0 ]; then
    echo "RPM package build failed."
fi
popd

