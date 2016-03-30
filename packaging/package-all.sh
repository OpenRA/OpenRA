#!/bin/bash
# OpenRA master packaging script

if [ $# -ne "2" ]; then
    echo "Usage: `basename $0` version outputdir"
    exit 1
fi

# Resolve the absolute source path from the location of this script
SRCDIR=$(readlink -f $(dirname $0)/../)
BUILTDIR="${SRCDIR}/packaging/built"
TAG=$1
OUTPUTDIR=$(readlink -f $2)

# Build the code and push the files into a clean dir
cd "$SRCDIR"
mkdir packaging/built
mkdir packaging/built/mods
make package

# Remove the mdb files that are created during `make`
find . -path "*.mdb" -delete

test -e Changelog.md && rm Changelog.md
curl -s -L -O https://raw.githubusercontent.com/wiki/OpenRA/OpenRA/Changelog.md

markdown Changelog.md > CHANGELOG.html
markdown README.md > README.html
markdown CONTRIBUTING.md > CONTRIBUTING.html
markdown DOCUMENTATION.md > DOCUMENTATION.html
markdown Lua-API.md > Lua-API.html

# List of files that are packaged on all platforms
FILES=('OpenRA.Game.exe' 'OpenRA.Game.exe.config' 'OpenRA.Utility.exe' 'OpenRA.Server.exe' 
'OpenRA.Platforms.Default.dll' \
'lua' 'glsl' 'mods/common' 'mods/ra' 'mods/cnc' 'mods/d2k' 'mods/modchooser' \
'AUTHORS' 'COPYING' 'README.html' 'CONTRIBUTING.html' 'DOCUMENTATION.html' 'CHANGELOG.html' \
'global mix database.dat' 'GeoLite2-Country.mmdb.gz')

echo "Copying files..."
for i in "${FILES[@]}"; do
    cp -R "${i}" "packaging/built/${i}" || exit 3
done

# SharpZipLib for zip file support
cp thirdparty/download/ICSharpCode.SharpZipLib.dll packaging/built

# FuzzyLogicLibrary for improved AI
cp thirdparty/download/FuzzyLogicLibrary.dll packaging/built

# SharpFont for FreeType support
cp thirdparty/download/SharpFont* packaging/built

# SDL2-CS
cp thirdparty/download/SDL2-CS* packaging/built

# OpenAL-CS
cp thirdparty/download/OpenAL-CS* packaging/built

# Mono.NAT for UPnP support
cp thirdparty/download/Mono.Nat.dll packaging/built

# Eluant (Lua integration)
cp thirdparty/download/Eluant* packaging/built

# GeoIP database access
cp thirdparty/download/MaxMind.Db.dll packaging/built
cp thirdparty/download/MaxMind.GeoIP2.dll packaging/built
cp thirdparty/download/Newtonsoft.Json.dll packaging/built
cp thirdparty/download/RestSharp.dll packaging/built

# global chat
cp thirdparty/download/SmarIrc4net.dll packaging/built

# Copy game icon for windows package
cp OpenRA.Game/OpenRA.ico packaging/built

# Copy the Windows crash monitor
cp OpenRA.exe packaging/built

cd packaging
echo "Creating packages..."

pushd windows >/dev/null
./buildpackage.sh "$TAG" "$BUILTDIR" "$SRCDIR" "$OUTPUTDIR"
if [ $? -ne 0 ]; then
    echo "Windows package build failed."
fi
popd >/dev/null

pushd osx >/dev/null
echo "Zipping OS X package"
./buildpackage.sh "$TAG" "$BUILTDIR" "$OUTPUTDIR"
if [ $? -ne 0 ]; then
    echo "OS X package build failed."
fi
popd >/dev/null

pushd linux >/dev/null
echo "Building Linux packages"
./buildpackage.sh "$TAG" "$BUILTDIR" "$OUTPUTDIR"
if [ $? -ne 0 ]; then
    echo "Linux package build failed."
fi
popd >/dev/null

echo "Package build done."

rm -rf $BUILTDIR
