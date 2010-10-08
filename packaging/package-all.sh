#!/bin/bash
# OpenRA master packaging script

if [ $# -ne "3" ]; then
	echo "Usage: `basename $0` tag srcdir outputdir"
    exit 1
fi

VERSION=`echo $1 | grep -o "[0-9]\\+-\\?[0-9]\\?"`
SRCDIR=$2
PACKAGEDIR=$3
BUILTDIR="${SRCDIR}/packaging/built"

# Build the code and push the files into a clean dir
cd "$SRCDIR"
echo $1 > VERSION
make game editor
mkdir packaging/built
mkdir packaging/built/mods

# List of files that are packaged on all platforms
# Note that the Tao dlls are shipped on all platforms except osx
# and that they are now installed to the game directory instead of being placed in the gac
FILES="OpenRA.Game.exe OpenRA.Editor.exe OpenRA.Gl.dll OpenRA.FileFormats.dll FreeSans.ttf FreeSansBold.ttf titles.ttf shaders mods/ra mods/cnc VERSION COPYING HACKING INSTALL"

# Files that match the above patterns, that should be excluded
EXCLUDE="*.mdb"

for i in $FILES; do
	cp -R "$i" "packaging/built/$i" || exit 3
done
for i in $EXCLUDE; do
	find . -path "$i" -delete
done

# Copy Tao
cp thirdparty/Tao/* packaging/built

# Copy WindowsBase.dll for linux packages
cp thirdparty/WindowsBase.dll packaging/built

# Copy game icon for windows package
cp OpenRA.Game/OpenRA.ico packaging/built

# Change into packaging directory and run the platform-dependant packaging in parallel
cd packaging

(
    echo "Building Windows package."
    cd windows
    makensis -DSRCDIR="$BUILTDIR" OpenRA.nsi &> package.log
    if [ $? -eq 0 ]; then
        mv OpenRA.exe "$PACKAGEDIR"/OpenRA-$VERSION.exe
    else
        echo "Windows package build failed, refer to windows/package.log."  
    fi
) &

(
    echo "Building OSX package."
	cd osx
    sh buildpackage.sh "$VERSION" "$BUILTDIR" "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "OSX package build failed, refer to osx/package.log."
    fi
) &

(
    echo "Building linux packages."
	cd linux
	sh buildpackage.sh "$VERSION" "$BUILTDIR" "$PACKAGEDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "linux package build failed, refer to linux/package.log."
    fi
) &
wait

rm -rf $BUILTDIR
