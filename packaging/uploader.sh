#!/bin/bash
# OpenRA Package uploader script
# Usage: uploader.sh <platform> <version string> <package file>
#    Will upload as user openra and prompt for a password if it's not defined in ~/.netrc

PLATFORM=$1
VERSION=$2
FILENAME=$3
FTPPATH=$4

FTP="ftp://$5:$6@${FTPSERVER}/${FTPPATH}/${PLATFORM}/"

if [ ! -e "${FILENAME}" ]; then
	echo "File not found: ${FILENAME}"
	exit 1
fi

SIZE=`du -bh ${FILENAME} | cut -f1`B
mkdir -p /tmp/${PLATFORM}/
echo -e "{\n\t\"version\":\"${VERSION}\",\n\t\"size\":\"${SIZE}\"\n}" > /tmp/${PLATFORM}/version.json
echo `basename ${FILENAME}` > /tmp/${PLATFORM}/latest.txt

pushd `dirname ${FILENAME}`
wput -u "${FTP}" "`basename ${FILENAME}`"
popd
pushd /tmp/${PLATFORM}
wput -u "${FTP}" version.json
wput -u "${FTP}" latest.txt
popd
