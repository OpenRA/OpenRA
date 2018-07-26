#!/bin/sh
# fallback without dependency resolution if nuget is not present on the system

####
# This file must stay /bin/sh and POSIX compliant for BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "Obtaining thirdparty dependencies requires curl or wget."; exit 1; }

archive="$1"
version="$2"
if command -v curl >/dev/null 2>&1; then
	curl -o "$archive.zip" -Ls https://nuget.org/api/v2/package/"$archive"/"$version"
else
	wget -cq https://nuget.org/api/v2/package/"$archive"/"$version" -O "$archive.zip"
fi
mkdir -p "$archive"
unzip -o -qq "$archive.zip" -d "$archive" && rm "$archive.zip"
