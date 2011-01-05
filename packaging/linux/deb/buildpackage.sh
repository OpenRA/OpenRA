#!/bin/bash
# OpenRA packaging script for Debian based distributions
E_BADARGS=85
if [ $# -ne "4" ]
then
    echo "Usage: `basename $0` version root-dir outputdir arch"
    exit $E_BADARGS
fi
VERSION=$1
rootdir=`readlink -f $2`
PACKAGE_SIZE=`du --apparent-size -c $rootdir/usr | grep "total" | awk '{print $1}'`
if [ $4 = "x64" ]
then
	ARCH=amd64
else
	ARCH=i386
fi

# Copy template files into a clean build directory (required)
mkdir root$ARCH
cp -R DEBIAN root$ARCH
cp -R $rootdir/usr root$ARCH

# Create the control and changelog files from templates
sed "s/{VERSION}/$VERSION/" DEBIAN/control | sed "s/{SIZE}/$PACKAGE_SIZE/" | sed "s/{ARCH}/$ARCH/" > root$ARCH/DEBIAN/control
sed "s/{VERSION}/$VERSION/" DEBIAN/changelog > root$ARCH/DEBIAN/changelog

# Build it in the temp directory, but place the finished deb in our starting directory
pushd root$ARCH

# Calculate md5sums and clean up the /usr/ part of them
md5sum `find . -type f | grep -v '^[.]/DEBIAN/'` | sed 's/\.\/usr\//usr\//g' > DEBIAN/md5sums

# Start building, the file should appear in the output directory
dpkg-deb -b . $3/openra-$VERSION-$ARCH.deb

# Clean up
popd
rm -rf root$ARCH

