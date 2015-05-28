#!/bin/bash

# ZBS binary directory
BIN_DIR="$(dirname "$PWD")/bin"

# temporary installation directory for dependencies
INSTALL_DIR="$PWD/deps"

# number of parallel jobs used for building
MAKEFLAGS="-j1" # some make may hang on Windows with j4 or j7

# flags for manual building with gcc
BUILD_FLAGS="-O2 -shared -s -I $INSTALL_DIR/include -L $INSTALL_DIR/lib"

# paths configuration
WXWIDGETS_BASENAME="wxWidgets"
WXWIDGETS_URL="http://svn.wxwidgets.org/svn/wx/wxWidgets/trunk"

WXLUA_BASENAME="wxlua"
WXLUA_URL="https://svn.code.sf.net/p/wxlua/svn/trunk"

LUASOCKET_BASENAME="luasocket-3.0-rc1"
LUASOCKET_FILENAME="v3.0-rc1.zip"
LUASOCKET_URL="https://github.com/diegonehab/luasocket/archive/$LUASOCKET_FILENAME"

OPENSSL_BASENAME="openssl-1.0.2a"
OPENSSL_FILENAME="$OPENSSL_BASENAME.tar.gz"
OPENSSL_URL="http://www.openssl.org/source/$OPENSSL_FILENAME"

LUASEC_BASENAME="luasec-0.5"
LUASEC_FILENAME="$LUASEC_BASENAME.zip"
LUASEC_URL="https://github.com/brunoos/luasec/archive/$LUASEC_FILENAME"

WINAPI_BASENAME="winapi"
WINAPI_URL="https://github.com/stevedonovan/winapi.git"

# exit if the command line is empty
if [ $# -eq 0 ]; then
  echo "Usage: $0 LIBRARY..."
  exit 0
fi

WXLUASTRIP="/strip"
WXWIDGETSDEBUG="--disable-debug"
WXLUABUILD="MinSizeRel"

# iterate through the command line arguments
for ARG in "$@"; do
  case $ARG in
  5.2)
    BUILD_52=true
    ;;
  5.3)
    BUILD_53=true
    BUILD_FLAGS="$BUILD_FLAGS -DLUA_COMPAT_APIINTCASTS"
    ;;
  jit)
    BUILD_JIT=true
    ;;
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
  debug)
    WXLUASTRIP=""
    WXWIDGETSDEBUG="--enable-debug=max --enable-debug_gdb"
    WXLUABUILD="Debug"
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

LUAV="51"
LUAD="5.1"
LUAS=""
LUA_BASENAME="lua-5.1.5"

if [ $BUILD_52 ]; then
  LUAV="52"
  LUAD="5.2"
  LUAS=$LUAV
  LUA_BASENAME="lua-5.2.2"
fi

LUA_FILENAME="$LUA_BASENAME.tar.gz"
LUA_URL="http://www.lua.org/ftp/$LUA_FILENAME"

if [ $BUILD_53 ]; then
  LUAV="53"
  LUAD="5.3"
  LUAS=$LUAV
  LUA_BASENAME="lua-5.3.0"
  LUA_FILENAME="$LUA_BASENAME.tar.gz"
  LUA_URL="http://www.lua.org/ftp/$LUA_FILENAME"
fi

if [ $BUILD_JIT ]; then
  LUA_BASENAME="LuaJIT-2.0.2"
  LUA_FILENAME="$LUA_BASENAME.tar.gz"
  LUA_URL="http://luajit.org/download/$LUA_FILENAME"
fi

# build wxWidgets
if [ $BUILD_WXWIDGETS ]; then
  svn co "$WXWIDGETS_URL" "$WXWIDGETS_BASENAME" || { echo "Error: failed to checkout wxWidgets"; exit 1; }
  svn revert -R "$WXWIDGETS_BASENAME"
  cd "$WXWIDGETS_BASENAME"
  ./configure --prefix="$INSTALL_DIR" $WXWIDGETSDEBUG --disable-shared --enable-unicode \
    --enable-compat28 \
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
  if [ $BUILD_JIT ]; then
    make CCOPT="-DLUAJIT_ENABLE_LUA52COMPAT" || { echo "Error: failed to build Lua"; exit 1; }
    make install PREFIX="$INSTALL_DIR"
    cp "$INSTALL_DIR/bin/luajit.exe" "$INSTALL_DIR/bin/lua.exe"
    # move luajit to lua as it's expected by luasocket and other components
    cp "$INSTALL_DIR"/include/luajit*/* "$INSTALL_DIR/include/"
  else
    make mingw || { echo "Error: failed to build Lua"; exit 1; }
    make install INSTALL_TOP="$INSTALL_DIR"
  fi
  cp src/lua$LUAV.dll "$INSTALL_DIR/lib"
  cp "$INSTALL_DIR/bin/lua.exe" "$INSTALL_DIR/bin/lua$LUAV.exe"
  [ -f "$INSTALL_DIR/lib/lua$LUAV.dll" ] || { echo "Error: lua$LUAV.dll isn't found"; exit 1; }
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

  # remove check for Lua 5.2 as it doesn't work with Twoface ABI mapper
  sed -i 's/LUA_VERSION_NUM < 502/0/' modules/wxlua/wxlcallb.cpp

  # (temporary) fix for compilation issue in wxlua in Windows using mingw (r184)
  sed -i 's/defined(__MINGW32__) || defined(__GNUWIN32__)/0/' modules/wxbind/src/wxcore_bind.cpp

  # (temporary) fix for compilation issue in wxlua using wxwidgets 3.1+ (r238)
  sed -i 's/{ "wxSTC_COFFEESCRIPT_HASHQUOTEDSTRING", wxSTC_COFFEESCRIPT_HASHQUOTEDSTRING },/\/\/ removed by ZBS build process/' modules/wxbind/src/wxstc_bind.cpp

  [ -f "$INSTALL_DIR/lib/libwxscintilla-3.0.a" ] && cp "$INSTALL_DIR/lib/libwxscintilla-3.0.a" "$INSTALL_DIR/lib/libwx_mswu_scintilla-3.0.a"
  [ -f "$INSTALL_DIR/lib/libwxscintilla-3.1.a" ] && cp "$INSTALL_DIR/lib/libwxscintilla-3.1.a" "$INSTALL_DIR/lib/libwx_mswu_scintilla-3.1.a"

  echo "set_target_properties(wxLuaModule PROPERTIES LINK_FLAGS -static)" >> modules/luamodule/CMakeLists.txt
  cmake -G "MSYS Makefiles" -DCMAKE_INSTALL_PREFIX="$INSTALL_DIR" -DCMAKE_BUILD_TYPE=$WXLUABUILD -DBUILD_SHARED_LIBS=FALSE \
    -DwxWidgets_CONFIG_EXECUTABLE="$INSTALL_DIR/bin/wx-config" \
    -DwxWidgets_COMPONENTS="stc;html;aui;adv;core;net;base" \
    -DwxLuaBind_COMPONENTS="stc;html;aui;adv;core;net;base" -DwxLua_LUA_LIBRARY_USE_BUILTIN=FALSE \
    -DwxLua_LUA_INCLUDE_DIR="$INSTALL_DIR/include" -DwxLua_LUA_LIBRARY="$INSTALL_DIR/lib/lua51.dll" .
  (cd modules/luamodule; make $MAKEFLAGS) || { echo "Error: failed to build wxLua"; exit 1; }
  (cd modules/luamodule; make install$WXLUASTRIP)
  [ -f "$INSTALL_DIR/bin/libwx.dll" ] || { echo "Error: libwx.dll isn't found"; exit 1; }
  cd ../..
  rm -rf "$WXLUA_BASENAME"
fi

# build LuaSocket
if [ $BUILD_LUASOCKET ]; then
  wget --no-check-certificate -c "$LUASOCKET_URL" -O "$LUASOCKET_FILENAME" || { echo "Error: failed to download LuaSocket"; exit 1; }
  unzip "$LUASOCKET_FILENAME"
  cd "$LUASOCKET_BASENAME"
  mkdir -p "$INSTALL_DIR/lib/lua/$LUAD/"{mime,socket}
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/$LUAD/mime/core.dll" src/mime.c -llua$LUAV \
    || { echo "Error: failed to build LuaSocket"; exit 1; }
  gcc $BUILD_FLAGS -DLUASOCKET_INET_PTON -D_WIN32_WINNT=0x0501 -o "$INSTALL_DIR/lib/lua/$LUAD/socket/core.dll" \
    src/{auxiliar.c,buffer.c,except.c,inet.c,io.c,luasocket.c,options.c,select.c,tcp.c,timeout.c,udp.c,wsocket.c} -lwsock32 -lws2_32 -llua$LUAV \
    || { echo "Error: failed to build LuaSocket"; exit 1; }
  mkdir -p "$INSTALL_DIR/share/lua/$LUAD/socket"
  cp src/{ftp.lua,http.lua,smtp.lua,tp.lua,url.lua} "$INSTALL_DIR/share/lua/$LUAD/socket"
  cp src/{ltn12.lua,mime.lua,socket.lua} "$INSTALL_DIR/share/lua/$LUAD"
  [ -f "$INSTALL_DIR/lib/lua/$LUAD/mime/core.dll" ] || { echo "Error: mime/core.dll isn't found"; exit 1; }
  [ -f "$INSTALL_DIR/lib/lua/$LUAD/socket/core.dll" ] || { echo "Error: socket/core.dll isn't found"; exit 1; }
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
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/$LUAD/ssl.dll" \
    src/luasocket/{timeout.c,buffer.c,io.c,wsocket.c} src/{context.c,x509.c,ssl.c} -Isrc -lssl -lcrypto -lws2_32 -lgdi32 -llua$LUAV \
    || { echo "Error: failed to build LuaSec"; exit 1; }
  cp src/ssl.lua "$INSTALL_DIR/share/lua/$LUAD"
  mkdir -p "$INSTALL_DIR/share/lua/$LUAD/ssl"
  cp src/https.lua "$INSTALL_DIR/share/lua/$LUAD/ssl"
  [ -f "$INSTALL_DIR/lib/lua/$LUAD/ssl.dll" ] || { echo "Error: luasec.dll isn't found"; exit 1; }
  cd ..
  rm -rf "$LUASEC_FILENAME" "$LUASEC_BASENAME"
fi

# build winapi
if [ $BUILD_WINAPI ]; then
  git clone "$WINAPI_URL" "$WINAPI_BASENAME"
  cd "$WINAPI_BASENAME"
  gcc $BUILD_FLAGS -DPSAPI_VERSION=1 -o "$INSTALL_DIR/lib/lua/$LUAD/winapi.dll" winapi.c wutils.c -lpsapi -lmpr -llua$LUAV \
    || { echo "Error: failed to build winapi"; exit 1; }
  [ -f "$INSTALL_DIR/lib/lua/$LUAD/winapi.dll" ] || { echo "Error: winapi.dll isn't found"; exit 1; }
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

[ $BUILD_LUA ] && cp "$INSTALL_DIR/bin/lua$LUAS.exe" "$INSTALL_DIR/lib/lua$LUAV.dll" "$BIN_DIR"
[ $BUILD_WXLUA ] && cp "$INSTALL_DIR/bin/libwx.dll" "$BIN_DIR/wx.dll"
[ $BUILD_WINAPI ] && cp "$INSTALL_DIR/lib/lua/$LUAD/winapi.dll" "$BIN_DIR/clibs$LUAS"
[ $BUILD_LUASEC ] && cp "$INSTALL_DIR/lib/lua/$LUAD/ssl.dll" "$BIN_DIR/clibs$LUAS"

if [ $BUILD_LUASOCKET ]; then
  mkdir -p "$BIN_DIR/clibs$LUAS/"{mime,socket}
  cp "$INSTALL_DIR/lib/lua/$LUAD/mime/core.dll" "$BIN_DIR/clibs$LUAS/mime"
  cp "$INSTALL_DIR/lib/lua/$LUAD/socket/core.dll" "$BIN_DIR/clibs$LUAS/socket"
fi

# To build lua5.1.dll proxy:
# (1) get mkforwardlib-gcc.lua from http://lua-users.org/wiki/LuaProxyDllThree
# (2) run it as "lua mkforwardlib-gcc.lua lua51 lua5.1 X86"
# To build lua5.2.dll proxy:
# (1) get mkforwardlib-gcc-52.lua from http://lua-users.org/wiki/LuaProxyDllThree
# (2) run it as "lua mkforwardlib-gcc-52.lua lua52 lua5.2 X86"

echo "*** Build has been successfully completed ***"
exit 0
