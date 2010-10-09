#!/bin/bash
# OpenRA packaging master script for linux packages

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` version files-dir outputdir"
    exit 1
fi

VERSION=$1
BUILTDIR=$2
PACKAGEDIR=$3

# Game files
mkdir -p root/usr/bin/
cp openra root/usr/bin/
mkdir -p root/usr/share/openra/
cp -R "$BUILTDIR/" "root/usr/share/openra/" || exit 3

# Desktop Icons
mkdir -p root/usr/share/applications/
sed "s/{VERSION}/$VERSION/" openra-ra.desktop > root/usr/share/applications/openra-ra.desktop
sed "s/{VERSION}/$VERSION/" openra-cnc.desktop > root/usr/share/applications/openra-cnc.desktop

# Menu entries
mkdir -p root/usr/share/menu/
cp openra-ra root/usr/share/menu/
cp openra-cnc root/usr/share/menu/

# Icon images
mkdir -p root/usr/share/pixmaps/
cp openra.32.xpm root/usr/share/pixmaps/
mkdir -p root/usr/share/icons/
cp -r hicolor root/usr/share/icons/

(
    echo "Building Debian package."
    cd deb
    ./buildpackage.sh "$VERSION" ../root "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "Debian package build failed, refer to $PWD/package.log."
    fi
) &

(
    echo "Building Arch-Linux package."
    cd pkgbuild
    sh buildpackage.sh "$VERSION" ../root "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "Arch-Linux package build failed, refer to $PWD/package.log."
    fi
) &
#     
#     (
#         echo "Building RPM package."
#         pushd rpm/ &> /dev/null
#         sh buildpackage.sh "$VERSION" ~/rpmbuild "$PACKAGEDIR" &> package.log
#         if [ $? -ne 0 ]; then
#             echo "RPM package build failed, refer to $PWD/package.log."
#         fi
#         popd &> /dev/null
#     ) &
# 
wait

# Clean up
rm -rf root