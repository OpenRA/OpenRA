#!/bin/bash
# OpenRA packaging script for Mac OSX
E_BADARGS=85
if [ $# -ne "3" ]
then
    echo "Usage: `basename $0` version root-dir outputdir"
    exit $E_BADARGS
fi
VERSION=$1
PACKAGE_SIZE=`du --apparent-size -c $2/usr | grep "total" | awk '{print $1}'`

# Copy template files into a clean build directory (required)
mkdir root
cp -R DEBIAN root
cp -R $2/usr root

# Create the control and changelog files from templates
sed "s/{VERSION}/$VERSION/" DEBIAN/control | sed "s/{SIZE}/$PACKAGE_SIZE/" > root/DEBIAN/control
sed "s/{VERSION}/$VERSION/" DEBIAN/changelog > root/DEBIAN/changelog

# Build it in the temp directory, but place the finished deb in our starting directory
pushd root

# Calculate md5sums and clean up the /usr/ part of them
md5sum `find . -type f | grep -v '^[.]/DEBIAN/'` | sed 's/\.\/usr\//usr\//g' > DEBIAN/md5sums

# Start building, the file should appear in the output directory
dpkg-deb -b . $3/openra-$VERSION.deb

# Clean up
popd
rm -rf root
