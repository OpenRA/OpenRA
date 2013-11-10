#!/bin/bash
# OpenRA packaging script for Debian based distributions

E_BADARGS=85
if [ $# -ne "3" ]
then
    echo "Usage: `basename $0` version root-dir outputdir"
    exit $E_BADARGS
fi
DATE=`echo $1 | grep -o "[0-9]\\+-\\?[0-9]\\?"`
TYPE=`echo $1 | grep -o "^[a-z]*"`
VERSION=$DATE.$TYPE

rootdir=`readlink -f $2`
PACKAGE_SIZE=`du --apparent-size -c $rootdir/usr | grep "total" | awk '{print $1}'`

# Copy template files into a clean build directory (required)
mkdir root
cp -R DEBIAN root
cp -R $rootdir/usr root

# Binaries go in /usr/games
mv root/usr/bin/ root/usr/games/
sed "s|/usr/bin|/usr/games|g" root/usr/games/openra > temp
mv temp root/usr/games/openra
chmod +x root/usr/games/openra
sed "s|/usr/bin|/usr/games|g" root/usr/games/openra-editor > temp
mv temp root/usr/games/openra-editor
chmod +x root/usr/games/openra-editor

# Put the copyright and changelog in /usr/share/doc/openra/
mkdir -p root/usr/share/doc/openra/
cp copyright root/usr/share/doc/openra/copyright
CHANGES=`cat ./root/usr/share/openra/CHANGELOG`
DATE=`date -R`

echo -e "openra (${VERSION}) unstable; urgency=low\n" > root/usr/share/doc/openra/changelog
cat ./root/usr/share/openra/CHANGELOG >> root/usr/share/doc/openra/changelog
echo -e "\n\n-- Paul Chote <sleipnir@sleipnirstuff.com> ${DATE}" >> root/usr/share/doc/openra/changelog
gzip -9 root/usr/share/doc/openra/changelog

# Create the control file
sed "s/{VERSION}/$VERSION/" DEBIAN/control | sed "s/{SIZE}/$PACKAGE_SIZE/" > root/DEBIAN/control

# Build it in the temp directory, but place the finished deb in our starting directory
pushd root

# Calculate md5sums and clean up the ./usr/ part of them
find . -type f -not -path "./DEBIAN/*" -print0 | xargs -0 -n1 md5sum | sed 's|\./usr/|/usr/|' > DEBIAN/md5sums
chmod 0644 DEBIAN/md5sums

# Replace any dashes in the version string with periods
PKGVERSION=`echo $1 | sed "s/-/\\./g"`

# Start building, the file should appear in the output directory
fakeroot dpkg-deb -b . $3/openra_${PKGVERSION}_all.deb

# Clean up
popd
rm -rf root

