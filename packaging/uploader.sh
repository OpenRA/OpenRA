#!/bin/bash
# OpenRA Package uploader script
# Usage: uploader.sh <platform> <version string> <package file>
#    Will upload as user openra and prompt for a password if it's not defined in ~/.netrc

PLATFORM=$1
VERSION=$2
FILENAME=$3

FTP="ftp://$4:$5@ftp.open-ra.org/httpdocs/releases/${PLATFORM}/"

if [ ! -e "${FILENAME}" ]; then
	echo "File not found: ${FILENAME}"
	exit 1
fi

SIZE=`du -bh ${FILENAME} | cut -f1`B
echo -e "{\n\t\"version\":\"${VERSION}\",\n\t\"size\":\"${SIZE}\"\n}" > /tmp/version.json
echo `basename ${FILENAME}` > /tmp/latest.txt

pushd `dirname ${FILENAME}`
wput "${FTP}" "`basename ${FILENAME}`"
popd
pushd /tmp/
wput "${FTP}" version.json
wput "${FTP}" latest.txt
popd
