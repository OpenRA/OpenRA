#!/bin/bash
# OpenRA packaging master script for linux packages

if [ $# -ne "4" ]; then
	echo "Usage: `basename $0` version files-dir outputdir arch"
    exit 1
fi

VERSION=$1
BUILTDIR=$2
PACKAGEDIR=$3
ROOTDIR=root$4

# Clean up
rm -rf $ROOTDIR

# Game files
mkdir -p $ROOTDIR/usr/bin/
if [ $4 = "x64" ]; then
	cp -T $BUILTDIR/gtklaunch $ROOTDIR/usr/bin/openra
else
	cp -T $BUILTDIR/gtklaunch32 $ROOTDIR/usr/bin/openra
fi

mkdir -p $ROOTDIR/usr/share/openra/
cp -R $BUILTDIR/* "$ROOTDIR/usr/share/openra/" || exit 3

# Remove unneeded files
rm $ROOTDIR/usr/share/openra/OpenRA.Launcher.exe
rm $ROOTDIR/usr/share/openra/gtklaunch
rm $ROOTDIR/usr/share/openra/gtklaunch32

# Desktop Icons
mkdir -p $ROOTDIR/usr/share/applications/
sed "s/{VERSION}/$VERSION/" openra.desktop > $ROOTDIR/usr/share/applications/openra.desktop

# Menu entries
mkdir -p $ROOTDIR/usr/share/menu/
cp openra $ROOTDIR/usr/share/menu/

# Icon images
mkdir -p $ROOTDIR/usr/share/pixmaps/
cp openra.32.xpm $ROOTDIR/usr/share/pixmaps/
mkdir -p $ROOTDIR/usr/share/icons/
cp -r hicolor $ROOTDIR/usr/share/icons/

(
    echo "Building Debian package."
    cd deb
    ./buildpackage.sh "$VERSION" ../$ROOTDIR "$PACKAGEDIR" $4 &> package.log
    if [ $? -ne 0 ]; then
        echo "Debian package build failed, refer to $PWD/package.log."
    fi
) &

if [ $4 = 'x86' ]
then
(
    echo "Building Arch-Linux package."
    cd pkgbuild
    sh buildpackage.sh "$VERSION" ../$ROOTDIR "$PACKAGEDIR" $4 &> package.log
    if [ $? -ne 0 ]; then
        echo "Arch-Linux package build failed, refer to $PWD/package.log."
    fi
) &
fi
     
(
    echo "Building RPM package."
    cd rpm
    sh buildpackage.sh "$VERSION" ../$ROOTDIR ~/rpmbuild "$PACKAGEDIR" $4 &> package.log
    if [ $? -ne 0 ]; then
        echo "RPM package build failed, refer to $PWD/package.log."
    fi
) &
 
wait

