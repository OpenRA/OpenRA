#!/bin/sh
# Download the IP2Location country database for use by the game server

####
# This file must stay /bin/sh and POSIX compliant for macOS and BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# Set the working directory to the location of this script
cd "$(dirname "$0")" || exit 1

# Database does not exist or is older than 30 days.
if [ -z "$(find . -path ./IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP -mtime -30 -print)" ]; then
	rm -f IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP
	echo "Downloading IP2Location GeoIP database."
	if command -v curl >/dev/null 2>&1; then
		curl -s -L -O https://download.ip2location.com/lite/IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP || echo "Warning: Download failed"
	else
		wget -cq https://download.ip2location.com/lite/IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP || echo "Warning: Download failed"
	fi
fi
