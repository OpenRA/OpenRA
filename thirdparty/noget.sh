#!/bin/bash
# fallback without dependency resolution if nuget is not present on the system

archive="$1"
version="$2"
curl -o "$archive.zip" -Ls https://nuget.org/api/v2/package/"$archive"/"$version"
mkdir -p "$archive"
unzip -o -qq "$archive.zip" -d "$archive" && rm "$archive.zip"