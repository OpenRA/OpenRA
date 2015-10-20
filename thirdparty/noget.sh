#!/bin/sh
# fallback without dependency resolution if nuget is not present on the system

####
# This file must stay /bin/sh and POSIX compliant for BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

archive="$1"
version="$2"
curl -o "$archive.zip" -Ls https://nuget.org/api/v2/package/"$archive"/"$version"
mkdir -p "$archive"
unzip -o -qq "$archive.zip" -d "$archive" && rm "$archive.zip"
