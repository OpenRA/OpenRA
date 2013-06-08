#!/bin/bash

# ZBS binary directory
BIN_DIR="$(dirname "$PWD")/bin"

# temporary installation directory for dependencies
INSTALL_DIR="$PWD/deps"

# number of parallel jobs used for building
MAKEFLAGS="-j4"

# flags for manual building with gcc
BUILD_FLAGS="-O2 -shared -s -I $INSTALL_DIR/include -L $INSTALL_DIR/lib"

# paths configuration
WXWIDGETS_BASENAME="wxWidgets"
WXWIDGETS_URL="http://svn.wxwidgets.org/svn/wx/wxWidgets/trunk"

LUA_BASENAME="lua-5.1.5"
LUA_FILENAME="$LUA_BASENAME.tar.gz"
LUA_URL="http://www.lua.org/ftp/$LUA_FILENAME"

WXLUA_BASENAME="wxlua"
WXLUA_URL="https://wxlua.svn.sourceforge.net/svnroot/wxlua/trunk"

LUASOCKET_BASENAME="luasocket-2.0.3"
LUASOCKET_FILENAME="$LUASOCKET_BASENAME-rc2.zip"
LUASOCKET_URL="https://github.com/downloads/diegonehab/luasocket/$LUASOCKET_FILENAME"

OPENSSL_BASENAME="openssl-1.0.1e"
OPENSSL_FILENAME="$OPENSSL_BASENAME.tar.gz"
OPENSSL_URL="http://www.openssl.org/source/$OPENSSL_FILENAME"

LUASEC_BASENAME="luasec-0.4.1"
LUASEC_FILENAME="$LUASEC_BASENAME.zip"
LUASEC_URL="https://github.com/brunoos/luasec/archive/$LUASEC_FILENAME"

WINAPI_BASENAME="winapi"
WINAPI_URL="https://github.com/stevedonovan/winapi.git"

# exit if the command line is empty
if [ $# -eq 0 ]; then
  echo "Usage: $0 LIBRARY..."
  exit 0
fi

# iterate through the command line arguments
for ARG in "$@"; do
  case $ARG in
  wxwidgets)
    BUILD_WXWIDGETS=true
    ;;
  lua)
    BUILD_LUA=true
    ;;
  wxlua)
    BUILD_WXLUA=true
    ;;
  luasocket)
    BUILD_LUASOCKET=true
    ;;
  luasec)
    BUILD_LUASEC=true
    ;;
  winapi)
    BUILD_WINAPI=true
    ;;
  zbstudio)
    BUILD_ZBSTUDIO=true
    ;;
  all)
    BUILD_WXWIDGETS=true
    BUILD_LUA=true
    BUILD_WXLUA=true
    BUILD_LUASOCKET=true
    BUILD_WINAPI=true
    BUILD_ZBSTUDIO=true
    ;;
  *)
    echo "Error: invalid argument $ARG"
    exit 1
    ;;
  esac
done

# check for g++
if [ ! "$(which g++)" ]; then
  echo "Error: g++ isn't found. Please install MinGW C++ compiler."
  exit 1
fi

# check for cmake
if [ ! "$(which cmake)" ]; then
  echo "Error: cmake isn't found. Please install CMake and add it to PATH."
  exit 1
fi

# check for svn
if [[ ($BUILD_WXWIDGETS || $BUILD_LUA) && ! "$(which svn)" ]]; then
  echo "Error: svn isn't found. Please install console SVN client."
  exit 1
fi

# check for git
if [[ $BUILD_WINAPI && ! "$(which git)" ]]; then
  echo "Error: git isn't found. Please install console GIT client."
  exit 1
fi

# check for wget
if [ ! "$(which wget)" ]; then
  # NOTE: can't check the return status since mingw-get always returns 0 even in the case of errors :(
  mingw-get install msys-wget
fi

# create the installation directory
mkdir -p "$INSTALL_DIR" || { echo "Error: cannot create directory $INSTALL_DIR"; exit 1; }

# build wxWidgets
if [ $BUILD_WXWIDGETS ]; then
  svn co "$WXWIDGETS_URL" "$WXWIDGETS_BASENAME" || { echo "Error: failed to checkout wxWidgets"; exit 1; }
  svn revert -R "$WXWIDGETS_BASENAME"
  cd "$WXWIDGETS_BASENAME"
  ./configure --prefix="$INSTALL_DIR" --disable-debug --disable-shared --enable-unicode \
    --with-libjpeg=builtin --with-libpng=builtin --with-libtiff=no --with-expat=no \
    --with-zlib=builtin --disable-richtext \
    CFLAGS="-Os -fno-keep-inline-dllexport" CXXFLAGS="-Os -fno-keep-inline-dllexport"
  make $MAKEFLAGS || { echo "Error: failed to build wxWidgets"; exit 1; }
  make install
  cd ..
  rm -rf "$WXWIDGETS_BASENAME"
fi

# build Lua
if [ $BUILD_LUA ]; then
  wget -c "$LUA_URL" -O "$LUA_FILENAME" || { echo "Error: failed to download Lua"; exit 1; }
  tar -xzf "$LUA_FILENAME"
  cd "$LUA_BASENAME"
  make mingw || { echo "Error: failed to build Lua"; exit 1; }
  make install INSTALL_TOP="$INSTALL_DIR"
  cp src/lua51.dll "$INSTALL_DIR/lib"
  [ -f "$INSTALL_DIR/lib/lua51.dll" ] || { echo "Error: lua51.dll isn't found"; exit 1; }
  cd ..
  rm -rf "$LUA_FILENAME" "$LUA_BASENAME"
fi

# build wxLua
if [ $BUILD_WXLUA ]; then
  svn co "$WXLUA_URL" "$WXLUA_BASENAME" || { echo "Error: failed to checkout wxLua"; exit 1; }
  svn revert -R "$WXLUA_BASENAME"
  cd "$WXLUA_BASENAME/wxLua"
  sed -i 's|:-/\(.\)/|:-\1:/|' "$INSTALL_DIR/bin/wx-config"
  sed -i 's/execute_process(COMMAND/& sh/' build/CMakewxAppLib.cmake modules/wxstedit/build/CMakewxAppLib.cmake
  # the following patches wxlua source to fix live coding support in wxlua apps
  # http://www.mail-archive.com/wxlua-users@lists.sourceforge.net/msg03225.html
  sed -i 's/\(m_wxlState = wxLuaState(wxlState.GetLuaState(), wxLUASTATE_GETSTATE|wxLUASTATE_ROOTSTATE);\)/\/\/ removed by ZBS build process \/\/ \1/' modules/wxlua/wxlcallb.cpp
  cp "$INSTALL_DIR/lib/libwxscintilla-2.9.a" "$INSTALL_DIR/lib/libwx_mswu_scintilla-2.9.a"
  echo "set_target_properties(wxLuaModule PROPERTIES LINK_FLAGS -static)" >> modules/luamodule/CMakeLists.txt
  cmake -G "MSYS Makefiles" -DBUILD_INSTALL_PREFIX="$INSTALL_DIR" -DCMAKE_BUILD_TYPE=MinSizeRel -DBUILD_SHARED_LIBS=FALSE \
    -DwxWidgets_CONFIG_EXECUTABLE="$INSTALL_DIR/bin/wx-config" \
    -DwxWidgets_COMPONENTS="stc;html;aui;adv;core;net;base" \
    -DwxLuaBind_COMPONENTS="stc;html;aui;adv;core;net;base" -DwxLua_LUA_LIBRARY_USE_BUILTIN=FALSE \
    -DwxLua_LUA_INCLUDE_DIR="$INSTALL_DIR/include" -DwxLua_LUA_LIBRARY="$INSTALL_DIR/lib/lua51.dll" .
  (cd modules/luamodule; make $MAKEFLAGS) || { echo "Error: failed to build wxLua"; exit 1; }
  (cd modules/luamodule; make install/strip)
  [ -f "$INSTALL_DIR/bin/libwx.dll" ] || { echo "Error: libwx.dll isn't found"; exit 1; }
  cd ../..
  rm -rf "$WXLUA_BASENAME"
fi

# build LuaSocket
if [ $BUILD_LUASOCKET ]; then
  wget --no-check-certificate -c "$LUASOCKET_URL" -O "$LUASOCKET_FILENAME" || { echo "Error: failed to download LuaSocket"; exit 1; }
  unzip "$LUASOCKET_FILENAME"
  cd "$LUASOCKET_BASENAME"
  mkdir -p "$INSTALL_DIR/lib/lua/5.1/"{mime,socket}
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/5.1/mime/core.dll" src/mime.c -llua51 \
    || { echo "Error: failed to build LuaSocket"; exit 1; }
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/5.1/socket/core.dll" \
    src/{auxiliar.c,buffer.c,except.c,inet.c,io.c,luasocket.c,options.c,select.c,tcp.c,timeout.c,udp.c,wsocket.c} -lwsock32 -llua51 \
    || { echo "Error: failed to build LuaSocket"; exit 1; }
  mkdir -p "$INSTALL_DIR/share/lua/5.1/socket"
  cp src/{ftp.lua,http.lua,smtp.lua,tp.lua,url.lua} "$INSTALL_DIR/share/lua/5.1/socket"
  cp src/{ltn12.lua,mime.lua,socket.lua} "$INSTALL_DIR/share/lua/5.1"
  [ -f "$INSTALL_DIR/lib/lua/5.1/mime/core.dll" ] || { echo "Error: mime/core.dll isn't found"; exit 1; }
  [ -f "$INSTALL_DIR/lib/lua/5.1/socket/core.dll" ] || { echo "Error: socket/core.dll isn't found"; exit 1; }
  cd ..
  rm -rf "$LUASOCKET_FILENAME" "$LUASOCKET_BASENAME"
fi

# build LuaSec
if [ $BUILD_LUASEC ]; then
  # build openSSL
  wget --no-check-certificate -c "$OPENSSL_URL" -O "$OPENSSL_FILENAME" || { echo "Error: failed to download OpenSSL"; exit 1; }
  tar -xzf "$OPENSSL_FILENAME"
  cd "$OPENSSL_BASENAME"
  bash Configure mingw
  make
  make install_sw INSTALLTOP="$INSTALL_DIR"
  cd ..
  rm -rf "$OPENSSL_FILENAME" "$OPENSSL_BASENAME"

  # build LuaSec
  wget --no-check-certificate -c "$LUASEC_URL" -O "$LUASEC_FILENAME" || { echo "Error: failed to download LuaSec"; exit 1; }
  unzip "$LUASEC_FILENAME"
  # the folder in the archive is "luasec-luasec-....", so need to fix
  mv "luasec-$LUASEC_BASENAME" $LUASEC_BASENAME
  cd "$LUASEC_BASENAME"
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/5.1/ssl.dll" \
    src/{timeout.c,buffer.c,io.c,context.c,ssl.c,wsocket.c} -lssl -lcrypto -lws2_32 -lgdi32 -llua51 \
    || { echo "Error: failed to build LuaSec"; exit 1; }
  cp src/{ssl.lua,https.lua} "$INSTALL_DIR/share/lua/5.1"
  [ -f "$INSTALL_DIR/lib/lua/5.1/ssl.dll" ] || { echo "Error: luasec.dll isn't found"; exit 1; }
  cd ..
  rm -rf "$LUASEC_FILENAME" "$LUASEC_BASENAME"
fi

# build winapi
if [ $BUILD_WINAPI ]; then
  git clone "$WINAPI_URL" "$WINAPI_BASENAME"
  cd "$WINAPI_BASENAME"
  gcc $BUILD_FLAGS -DPSAPI_VERSION=1 -o "$INSTALL_DIR/lib/lua/5.1/winapi.dll" winapi.c wutils.c -lpsapi -lmpr -llua51 \
    || { echo "Error: failed to build winapi"; exit 1; }
  [ -f "$INSTALL_DIR/lib/lua/5.1/winapi.dll" ] || { echo "Error: winapi.dll isn't found"; exit 1; }
  cd ..
  rm -rf "$WINAPI_BASENAME"
fi

# build ZBS launcher
if [ $BUILD_ZBSTUDIO ]; then
  windres ../zbstudio/res/zbstudio.rc zbstudio.rc.o
  gcc -O2 -s -mwindows -o ../zbstudio.exe win32_starter.c zbstudio.rc.o
  rm zbstudio.rc.o
  [ -f ../zbstudio.exe ] || { echo "Error: zbstudio.exe isn't found"; exit 1; }
fi

# now copy the compiled dependencies to ZBS binary directory
mkdir -p "$BIN_DIR" || { echo "Error: cannot create directory $BIN_DIR"; exit 1; }
[ $BUILD_LUA ] && cp "$INSTALL_DIR/bin/lua.exe" "$INSTALL_DIR/lib/lua51.dll" "$BIN_DIR"
[ $BUILD_WXLUA ] && cp "$INSTALL_DIR/bin/libwx.dll" "$BIN_DIR/wx.dll"
[ $BUILD_WINAPI ] && cp "$INSTALL_DIR/lib/lua/5.1/winapi.dll" "$BIN_DIR"
[ $BUILD_LUASEC ] && cp "$INSTALL_DIR/lib/lua/5.1/ssl.dll" "$BIN_DIR"
if [ $BUILD_LUASOCKET ]; then
  mkdir -p "$BIN_DIR/clibs/"{mime,socket}
  cp "$INSTALL_DIR/lib/lua/5.1/mime/core.dll" "$BIN_DIR/clibs/mime"
  cp "$INSTALL_DIR/lib/lua/5.1/socket/core.dll" "$BIN_DIR/clibs/socket"
fi

# To build lua5.1.dll proxy:
# (1) get mkforwardlib-gcc.lua from http://lua-users.org/wiki/LuaProxyDllThree
# (2) run it as "lua mkforwardlib-gcc.lua lua51 lua5.1 X86"

# show a message about successful completion
echo "*** Build has been successfully completed ***"
exit 0
