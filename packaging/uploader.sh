#!/bin/sh
# OpenRA Package uploader script
# Usage: uploader.sh <platform> <version string> <package file>
#    Will upload as user openra and prompt for a password if it's not defined in ~/.netrc

PLATFORM=$1
VERSION=$2
FILENAME=$3
FTP="ftp://openra@open-ra.org/httpdocs/releases/${PLATFORM}/"

if [ ! -e "${FILENAME}" ]; then
	echo "File not found: ${FILENAME}"
	exit 1
fi

SIZE=`ls -lh "${FILENAME}" | awk '{ print $5 }'`B
echo "{\n\t\"version\":\"${VERSION}\",\n\t\"size\":\"${SIZE}\"\n}" > version.json
echo `basename ${FILENAME}` > latest.txt

ftp "${FTP}" <<EOT
pwd
put "${FILENAME}" "`basename ${FILENAME}`"
put version.json
put latest.txt
quit
EOT
