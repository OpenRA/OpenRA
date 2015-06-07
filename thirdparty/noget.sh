#!/bin/sh
# fallback without dependency resolution if nuget is not present on the system

. ../configure-utils.sh

archive="$1"
version="$2"
download "$archive.zip" https://nuget.org/api/v2/package/"$archive"/"$version"
mkdir -p "$archive"
unzip -o -qq "$archive.zip" -d "$archive" && rm "$archive.zip"