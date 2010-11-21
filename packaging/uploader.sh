#!/bin/bash
# OpenRA Package uploader script
# Usage: uploader.sh <platform> <version string> <filename> <ftp path> <username> <password>

PLATFORM=$1
VERSION=$2
FILENAME=$3
LATESTNAME=$4
FTPSERVER=$5
FTPPATH=$6

FTP="ftp://$7:$8@${FTPSERVER}/${FTPPATH}/${PLATFORM}/"

if [ ! -e "${FILENAME}" ]; then
	echo "File not found: ${FILENAME}"
	exit 1
fi

SIZE=`du -bh ${FILENAME} | cut -f1`B
mkdir -p /tmp/${PLATFORM}/
echo -e "{\n\t\"version\":\"${VERSION}\",\n\t\"size\":\"${SIZE}\"\n}" > /tmp/${PLATFORM}/version.json
echo `basename ${FILENAME}` > /tmp/${PLATFORM}/${LATESTNAME}.txt

pushd `dirname ${FILENAME}`
wput -u "${FTP}" "`basename ${FILENAME}`"
popd
pushd /tmp/${PLATFORM}
wput -u "${FTP}" version.json
wput -u "${FTP}" ${LATESTNAME}.txt
popd
