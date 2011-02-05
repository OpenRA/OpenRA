#!/bin/bash
# OpenRA Package uploader script
# Usage: uploader.sh <platform> <filename> <ftp server> <ftp path> <username> <password>

PLATFORM=$1
FILENAME=$2
FTPSERVER=$3
FTPPATH=$4

FTP="ftp://$7:$8@${FTPSERVER}/${FTPPATH}/${PLATFORM}/"

if [ ! -e "${FILENAME}" ]; then
	echo "File not found: ${FILENAME}"
	exit 1
fi

pushd `dirname ${FILENAME}`
wput -u "${FTP}" "`basename ${FILENAME}`"
popd
