#!/bin/bash
# OpenRA packaging master script for linux packages

command -v curl >/dev/null 2>&1 || { echo >&2 "Linux packaging requires curl."; exit 1; }
command -v markdown >/dev/null 2>&1 || { echo >&2 "Linux packaging requires markdown."; exit 1; }

if [ $# -ne "2" ]; then
	echo "Usage: `basename $0` tag outputdir"
    exit 1
fi

# Set the working dir to the location of this script
cd $(dirname $0)

TAG="${1}"
VERSION=`echo ${TAG} | grep -o "[0-9]\\+-\\?[0-9]\\?"`
OUTPUTDIR="${2}"

SRCDIR="$(pwd)/../.."
BUILTDIR="$(pwd)/build"

# Clean up
rm -rf ${BUILTDIR}

echo "Building core files"

pushd ${SRCDIR} > /dev/null
make linux-dependencies
make core SDK="-sdk:4.5"
make version VERSION="${TAG}"

make install-core prefix="/usr" DESTDIR="${BUILTDIR}"
make install-linux-shortcuts prefix="/usr" DESTDIR="${BUILTDIR}"
make install-linux-mime prefix="/usr" DESTDIR="${BUILTDIR}"
make install-linux-appdata prefix="/usr" DESTDIR="${BUILTDIR}"
make install-man-page prefix="/usr" DESTDIR="${BUILTDIR}"

popd > /dev/null

# Documentation
DOCSDIR="${BUILTDIR}/usr/share/doc/openra-${TAG}/"

mkdir -p "${DOCSDIR}"

curl -s -L -O https://raw.githubusercontent.com/wiki/OpenRA/OpenRA/Changelog.md
markdown Changelog.md > "${DOCSDIR}/Changelog.html"
rm Changelog.md

markdown ${SRCDIR}/README.md > "${DOCSDIR}/README.html"
markdown ${SRCDIR}/CONTRIBUTING.md > "${DOCSDIR}/CONTRIBUTING.html"

pushd deb >/dev/null
echo "Building Debian package"
./buildpackage.sh "${TAG}" "${BUILTDIR}" "${OUTPUTDIR}"
if [ $? -ne 0 ]; then
    echo "Debian package build failed."
fi
popd >/dev/null
rm -rf "${BUILTDIR}"
