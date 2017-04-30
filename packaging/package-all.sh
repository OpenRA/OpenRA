#!/bin/bash
# OpenRA master packaging script

if [ $# -ne "2" ]; then
    echo "Usage: `basename $0` version outputdir"
    exit 1
fi

# Set the working dir to the location of this script
cd $(dirname $0)

pushd windows >/dev/null
echo "Building Windows package"
./buildpackage.sh "$1" "$2"
if [ $? -ne 0 ]; then
    echo "Windows package build failed."
fi
popd >/dev/null

pushd osx >/dev/null
echo "Building macOS package"
./buildpackage.sh "$1" "$2"
if [ $? -ne 0 ]; then
    echo "macOS package build failed."
fi
popd >/dev/null

pushd linux >/dev/null
echo "Building Linux packages"
./buildpackage.sh "$1" "$2"
if [ $? -ne 0 ]; then
    echo "Linux package build failed."
fi
popd >/dev/null

echo "Package build done."
