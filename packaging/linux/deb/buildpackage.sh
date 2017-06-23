#!/bin/bash
# OpenRA packaging script for Debian based distributions

command -v fakeroot >/dev/null 2>&1 || { echo >&2 "Debian packaging requires fakeroot."; exit 1; }
command -v dpkg-deb >/dev/null 2>&1 || { echo >&2 "Debian packaging requires dpkg-deb."; exit 1; }

if [ $# -ne "3" ]
then
    echo "Usage: `basename $0` version root-dir outputdir"
    exit $E_BADARGS
fi

# Set the working dir to the location of this script
cd $(dirname $0)

TAG="${1}"
LINUX_BUILD_ROOT="${2}"
OUTPUTDIR="${3}"

DEB_BUILD_ROOT="$(pwd)/build"

LIBDIR=/usr/lib/openra
DOCDIR=/usr/share/doc/openra
SYSTEMSUPPORTDIR=/var/games/openra
LINTIANORDIR=/usr/share/lintian/overrides
E_BADARGS=85

DATE=`echo ${TAG} | grep -o "[0-9]\\+-\\?[0-9]\\?"`
TYPE=`echo $1 | grep -o "^[a-z]*"`
VERSION="${DATE}.${TYPE}"

# Copy template files into a clean build directory (required)
mkdir "${DEB_BUILD_ROOT}"
cp -R DEBIAN "${DEB_BUILD_ROOT}"
cp -R "${LINUX_BUILD_ROOT}/usr" "${DEB_BUILD_ROOT}"
cp -R Eluant.dll.config "${DEB_BUILD_ROOT}/${LIBDIR}/"
chmod 0644 "${DEB_BUILD_ROOT}/${LIBDIR}/"*.dll
chmod 0644 "${DEB_BUILD_ROOT}/${LIBDIR}/"*/**/*.dll

# Binaries go in /usr/games
mv "${DEB_BUILD_ROOT}/usr/bin/" "${DEB_BUILD_ROOT}/usr/games/"
sed "s|/usr/bin|/usr/games|g" "${DEB_BUILD_ROOT}/usr/games/openra-ra" > temp
mv -f temp "${DEB_BUILD_ROOT}/usr/games/openra-ra"
sed "s|/usr/bin|/usr/games|g" "${DEB_BUILD_ROOT}/usr/games/openra-cnc" > temp
mv -f temp "${DEB_BUILD_ROOT}/usr/games/openra-cnc"
sed "s|/usr/bin|/usr/games|g" "${DEB_BUILD_ROOT}/usr/games/openra-d2k" > temp
mv -f temp "${DEB_BUILD_ROOT}/usr/games/openra-d2k"

chmod 0755 "${DEB_BUILD_ROOT}/usr/games/openra"*

# Compress the man page
gzip -9n "${DEB_BUILD_ROOT}/usr/share/man/man6/openra.6"

# Put the copyright and changelog in /usr/share/doc/openra/
mkdir -p "${DEB_BUILD_ROOT}/${DOCDIR}"
cp copyright "${DEB_BUILD_ROOT}/${DOCDIR}/copyright"
cp "${DEB_BUILD_ROOT}/${LIBDIR}/AUTHORS" "${DEB_BUILD_ROOT}/${DOCDIR}"
gzip -9 "${DEB_BUILD_ROOT}/${DOCDIR}/AUTHORS"
DATE=`date -R`

# Put the lintian overrides in /usr/share/lintian/overrides/
mkdir -p "${DEB_BUILD_ROOT}/${LINTIANORDIR}"
cp openra.lintian-overrides "${DEB_BUILD_ROOT}/${LINTIANORDIR}/openra"

echo -e "openra (${VERSION}) unstable; urgency=low\n" > "${DEB_BUILD_ROOT}/${DOCDIR}/changelog"
echo -e "  * New upstream release: $TAG" >> "${DEB_BUILD_ROOT}/${DOCDIR}/changelog"
echo -e "\n -- Paul Chote <paul@chote.net>  ${DATE}" >> "${DEB_BUILD_ROOT}/${DOCDIR}/changelog"
gzip -9 "${DEB_BUILD_ROOT}/${DOCDIR}/changelog"
rm "${DEB_BUILD_ROOT}/${LIBDIR}/COPYING"

# Nothing should have group writable permissions
# (These might occur due to difference in umask values.)
chmod -R g-w "${DEB_BUILD_ROOT}"

# Create the control file
if [[ "$OSTYPE" == "darwin"* ]]; then
	# BSD du doesn't have an --apparent-size flag, so we must accept a different result
	PACKAGE_SIZE=`du -c "${DEB_BUILD_ROOT}/usr" | grep "total" | awk '{print $1}'`
else
	PACKAGE_SIZE=`du --apparent-size -c "${DEB_BUILD_ROOT}/usr" | grep "total" | awk '{print $1}'`
fi

sed "s/{VERSION}/${VERSION}/" DEBIAN/control | sed "s/{SIZE}/${PACKAGE_SIZE}/" > "${DEB_BUILD_ROOT}/DEBIAN/control"
sed "s|{LIBDIR}|${LIBDIR}|g" DEBIAN/postinst | sed "s|{SYSTEMSUPPORTDIR}|${SYSTEMSUPPORTDIR}|g" > "${DEB_BUILD_ROOT}/DEBIAN/postinst"
sed "s|{LIBDIR}|${LIBDIR}|g" DEBIAN/prerm | sed "s|{SYSTEMSUPPORTDIR}|${SYSTEMSUPPORTDIR}|g" > "${DEB_BUILD_ROOT}/DEBIAN/prerm"
chmod 0755 "${DEB_BUILD_ROOT}/DEBIAN/postinst" "${DEB_BUILD_ROOT}/DEBIAN/prerm"

# Build it in the temp directory, but place the finished deb in our starting directory
pushd "${DEB_BUILD_ROOT}" >/dev/null

# Calculate md5sums and clean up the ./usr/ part of them
if [[ "$OSTYPE" == "darwin"* ]]; then
	find . -type f -not -path "./DEBIAN/*" -print0 | xargs -0 -n1 openssl md5 | awk '{ print $2, substr($1, 7, length($1)-8) }' > DEBIAN/md5sums
else
	find . -type f -not -path "./DEBIAN/*" -print0 | xargs -0 -n1 md5sum | sed 's|\./usr/|usr/|' > DEBIAN/md5sums	
fi
 
chmod 0644 DEBIAN/md5sums

# Replace any dashes in the version string with periods
PKGVERSION=`echo ${TAG} | sed "s/-/\\./g"`

# Start building, the file should appear in the output directory
fakeroot dpkg-deb -b . "${OUTPUTDIR}/openra_${PKGVERSION}_all.deb"

# Clean up
popd >/dev/null
rm -rf "${DEB_BUILD_ROOT}"

