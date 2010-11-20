#!/bin/bash
# OpenRA master packaging script

if [ $# -ne "2" ]; then
	echo "Usage: `basename $0` version outputdir"
    exit 1
fi

# Resolve the absolute source path from the location of this script
SRCDIR=$(readlink -f $(dirname $0)/../)
BUILTDIR="${SRCDIR}/packaging/built"
VERSION=`echo $1 | grep -o "[0-9]\\+-\\?[0-9]\\?"`
OUTPUTDIR=$(readlink -f $2)

# Build the code and push the files into a clean dir
cd "$SRCDIR"
mkdir packaging/built
mkdir packaging/built/mods
echo $1 > VERSION
make package

# Remove the mdb files that are created during `make`
find . -path "*.mdb" -delete

# List of files that are packaged on all platforms
# Note that the Tao dlls are shipped on all platforms except osx and that
# they are now installed to the game directory instead of placed in the gac
FILES="OpenRA.Game.exe OpenRA.Editor.exe OpenRA.Utility.exe OpenRA.Renderer.Cg.dll OpenRA.Renderer.Gl.dll OpenRA.Renderer.Null.dll \
OpenRA.FileFormats.dll FreeSans.ttf FreeSansBold.ttf titles.ttf cg glsl mods/ra mods/cnc VERSION COPYING HACKING INSTALL"

echo "Copying files..."
for i in $FILES; do
	cp -R "$i" "packaging/built/$i" || exit 3
done

# Copy Tao
cp thirdparty/Tao/* packaging/built

# SharpZipLib for zip file support
cp thirdparty/ICSharpCode.SharpZipLib.dll packaging/built

# Copy game icon for windows package
cp OpenRA.Game/OpenRA.ico packaging/built

#
# Change into packaging directory and run the 
# platform-dependant packaging in parallel
#
cd packaging
echo "Creating packages..."

(
    cd windows
    makensis -DSRCDIR="$BUILTDIR" OpenRA.nsi &> package.log
    if [ $? -eq 0 ]; then
        mv OpenRA.exe "$OUTPUTDIR"/OpenRA-$VERSION.exe
    else
        echo "Windows package build failed, refer to windows/package.log."  
    fi
) &

(
    cd osx
    sh buildpackage.sh "$VERSION" "$BUILTDIR" "$OUTPUTDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "OSX package build failed, refer to osx/package.log."
    fi
) &

(
    cd linux
    sh buildpackage.sh "$VERSION" "$BUILTDIR" "$OUTPUTDIR" &> package.log
    if [ $? -ne 0 ]; then
        echo "linux package build failed, refer to linux/package.log."
    fi
) &
wait

rm -rf $BUILTDIR
