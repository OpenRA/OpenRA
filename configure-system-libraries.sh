#!/bin/sh
# Patch *.dll.config files to use system libraries, working around issues with directories and naming schemes

####
# This file must stay /bin/sh and POSIX compliant for macOS and BSD portability.
# Copy-paste the entire script into https://shellcheck.net to check.
####

set -o errexit || exit $?

create_symlinks()
{
	LABEL=$1
	SEARCHDIRS=$2
	REPLACE=$3
	SEARCH=$4

	# Exit early if the symlink already exists
	if [ -L "bin/${REPLACE}" ]; then
		return 0
	fi

	printf "Searching for %s... " "${LABEL}"
	for DIR in ${SEARCHDIRS} ; do
		for LIB in ${SEARCH}; do
			if [ -f "${DIR}/${LIB}" ]; then
				echo "${LIB}"
				ln -s "${DIR}/${LIB}" "bin/${REPLACE}"
				return 0
			fi
		done
	done

	echo "FAILED"

	echo "OpenRA expects to find a file matching \"${SEARCH}\" in one of the following locations:"
	echo "${SEARCHDIRS}"
	exit 1
}

if [ "$(uname -s)" = "Darwin" ]; then
	if [ "$(arch)" = "arm64" ]; then
		SEARCHDIRS="/opt/homebrew/lib /opt/homebrew/opt/openal-soft/lib"
	else
		SEARCHDIRS="/usr/local/lib /usr/local/opt/openal-soft/lib"
	fi
	create_symlinks "Lua 5.1" "${SEARCHDIRS}" lua51.dylib liblua5.1.dylib
	create_symlinks SDL2 "${SEARCHDIRS}" SDL2.dylib libSDL2-2.0.0.dylib
	create_symlinks OpenAL "${SEARCHDIRS}" soft_oal.dylib libopenal.1.dylib
	create_symlinks FreeType "${SEARCHDIRS}" freetype6.dylib libfreetype.6.dylib
else
	SEARCHDIRS="/lib /lib64 /usr/lib /usr/lib64 /usr/lib/x86_64-linux-gnu /usr/lib/i386-linux-gnu /usr/lib/arm-linux-gnueabihf /usr/lib/aarch64-linux-gnu /usr/lib/powerpc64le-linux-gnu /usr/lib/mipsel-linux-gnu /usr/local/lib /opt/lib /opt/local/lib /app/lib"
	create_symlinks "Lua 5.1" "${SEARCHDIRS}" lua51.so "liblua.so.5.1.5 liblua5.1.so.5.1 liblua5.1.so.0 liblua.so.5.1 liblua-5.1.so liblua5.1.so"
	create_symlinks SDL2 "${SEARCHDIRS}" SDL2.so "libSDL2-2.0.so.0 libSDL2-2.0.so libSDL2.so"
	create_symlinks OpenAL "${SEARCHDIRS}" soft_oal.so "libopenal.so.1 libopenal.so"
	create_symlinks FreeType "${SEARCHDIRS}" freetype6.so "libfreetype.so.6 libfreetype.so"
fi
