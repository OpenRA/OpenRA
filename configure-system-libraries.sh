#!/bin/sh
# Patch *.dll.config files to use system libraries, working around issues with directories and naming schemes

####
# This file must stay /bin/sh and POSIX compliant for macOS and BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

patch_config()
{
	LABEL=$1
	SEARCHDIRS=$2
	CONFIG=$3
	REPLACE=$4
	SEARCH=$5

	# Exit early if the file has already been patched
	grep -q "target=\"${REPLACE}\"" "${CONFIG}" || return 0

	printf "Searching for %s... " "${LABEL}"
	for DIR in ${SEARCHDIRS} ; do
		for LIB in ${SEARCH}; do
			if [ -f "${DIR}/${LIB}" ]; then
				echo "${LIB}"
				sed "s|target=\"${REPLACE}\"|target=\"${DIR}/${LIB}\"|" "${CONFIG}" > "${CONFIG}.temp"
				mv "${CONFIG}.temp" "${CONFIG}"
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
	SEARCHDIRS="/usr/local/lib /usr/local/opt/openal-soft/lib"
	patch_config "Lua 5.1" "${SEARCHDIRS}" bin/Eluant.dll.config lua51.dylib liblua5.1.dylib
	patch_config SDL2 "${SEARCHDIRS}" bin/SDL2-CS.dll.config SDL2.dylib libSDL2-2.0.0.dylib
	patch_config OpenAL "${SEARCHDIRS}" bin/OpenAL-CS.Core.dll.config soft_oal.dylib libopenal.1.dylib
	patch_config FreeType "${SEARCHDIRS}" bin/OpenRA.Platforms.Default.dll.config freetype6.dylib libfreetype.6.dylib
else
	SEARCHDIRS="/lib /lib64 /usr/lib /usr/lib64 /usr/lib/i386-linux-gnu /usr/lib/x86_64-linux-gnu /usr/lib/arm-linux-gnueabihf /usr/lib/aarch64-linux-gnu /usr/lib/powerpc64le-linux-gnu /usr/lib/mipsel-linux-gnu /usr/local/lib /opt/lib /opt/local/lib /app/lib"
	patch_config "Lua 5.1" "${SEARCHDIRS}" bin/Eluant.dll.config lua51.so "liblua.so.5.1.5 liblua5.1.so.5.1 liblua5.1.so.0 liblua.so.5.1 liblua-5.1.so liblua5.1.so"
	patch_config SDL2 "${SEARCHDIRS}" bin/SDL2-CS.dll.config SDL2.so "libSDL2-2.0.so.0 libSDL2-2.0.so libSDL2.so"
	patch_config OpenAL "${SEARCHDIRS}" bin/OpenAL-CS.Core.dll.config soft_oal.so "libopenal.so.1 libopenal.so"
	patch_config FreeType "${SEARCHDIRS}" bin/OpenRA.Platforms.Default.dll.config freetype6.so "libfreetype.so.6 libfreetype.so"
fi
