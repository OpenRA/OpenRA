#!/bin/bash
# OpenRA packaging master script for linux packages

if [ $# -ne "4" ]; then
	echo "Usage: `basename $0` version files-dir outputdir arch"
    exit 1
fi

VERSION=$1
BUILTDIR=$2
PACKAGEDIR=$3

# Clean up
rm -rf root

# Game files
mkdir -p root/usr/bin/
if [ $4 = "x64" ]; then
	cp -T $BUILTDIR/gtklaunch root/usr/bin/openra
else
	cp -T $BUILTDIR/gtklaunch32 root/usr/bin/openra
fi

mkdir -p root/usr/share/openra/
cp -R $BUILTDIR/* "root/usr/share/openra/" || exit 3

# Remove unneeded files
rm root/usr/share/openra/OpenRA.Launcher.exe
rm root/usr/share/openra/gtklaunch
rm root/usr/share/openra/gtklaunch32

# Desktop Icons
mkdir -p root/usr/share/applications/
sed "s/{VERSION}/$VERSION/" openra.desktop > root/usr/share/applications/openra.desktop

# Menu entries
mkdir -p root/usr/share/menu/
cp openra root/usr/share/menu/

# Icon images
mkdir -p root/usr/share/pixmaps/
cp openra.32.xpm root/usr/share/pixmaps/
mkdir -p root/usr/share/icons/
cp -r hicolor root/usr/share/icons/

(
    echo "Building Debian package."
    cd deb
    ./buildpackage.sh "$VERSION" ../root "$PACKAGEDIR" $4 &> package.log
    if [ $? -ne 0 ]; then
        echo "Debian package build failed, refer to $PWD/package.log."
    fi
) &

(
    echo "Building Arch-Linux package."
    cd pkgbuild
    sh buildpackage.sh "$VERSION" ../root "$PACKAGEDIR" $4 &> package.log
    if [ $? -ne 0 ]; then
        echo "Arch-Linux package build failed, refer to $PWD/package.log."
    fi
) &
     
(
    echo "Building RPM package."
    cd rpm
    sh buildpackage.sh "$VERSION" ../root ~/rpmbuild "$PACKAGEDIR" $4 &> package.log
    if [ $? -ne 0 ]; then
        echo "RPM package build failed, refer to $PWD/package.log."
    fi
) &
 
wait

