#!/bin/sh

# Die on any error for Travis CI to automatically retry:
set -e

download_dir="${0%/*}/download"
mkdir -p "${download_dir}"
cd "${download_dir}"

filename="GeoLite2-Country.mmdb.gz"

# Database does not exist or is older than 30 days.
if [ ! -e $filename ] || [ -n "$(find . -name $filename -mtime +30 -print)" ]; then
	rm -f $filename
	echo "Updating GeoIP country database from MaxMind."
	curl -s -L -O http://geolite.maxmind.com/download/geoip/database/$filename
fi
