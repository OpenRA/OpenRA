#!/bin/sh

cd "`dirname "\`readlink -f "$0"\`"`" 

if [ -e "desura_prelaunch.sh" ]; then
	. desura_prelaunch.sh
fi

BIN_PATH="$HOME/.desura/games/openra/launch-editor.sh"
ARGS=
# TODO: This is crap.
libSDL_Path="$HOME/.desura/tools/63/"
libopenal_Path="$HOME/.desura/tools/73/"
libCgGL_PATH="$HOME/.desura/tools/78/"
libCg_PATH="$HOME/.desura/tools/79/"
libfreetype_Path="$HOME/.desura/tools/92/"
libGLU_Path="$HOME/.desura/tools/93/"
libalut_Path="$HOME/.desura/tools/195/"

if [ -n $ARGS ]; then
	LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:${libSDL_Path}:${libopenal_Path}:${libCgGL_PATH}:${libCg_PATH}:${libfreetype_Path}:${libGLU_Path}:${libalut_Path} ${BIN_PATH} ${ARGS} $@
else
	LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:${libSDL_Path}:${libopenal_Path}:${libCgGL_PATH}:${libCg_PATH}:${libfreetype_Path}:${libGLU_Path}:${libalut_Path} ${BIN_PATH} $@
fi