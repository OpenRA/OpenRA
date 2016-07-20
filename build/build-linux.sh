#!/bin/bash

# exit if the command line is empty
if [ $# -eq 0 ]; then
  echo "Usage: $0 LIBRARY..."
  exit 0
fi

case "$(uname -m)" in
	x86_64)
		FPIC="-fpic"
		ARCH="x64"
		;;
	armv7l)
		FPIC="-fpic"
		ARCH="armhf"
		;;
	*)
		FPIC=""
		ARCH="x86"
		;;
esac

# binary directory
BIN_DIR="$(dirname "$PWD")/bin/linux/$ARCH"

# temporary installation directory for dependencies
INSTALL_DIR="$PWD/deps"

# number of parallel jobs used for building
MAKEFLAGS="-j4"

# flags for manual building with gcc
BUILD_FLAGS="-O2 -shared -s -I $INSTALL_DIR/include -L $INSTALL_DIR/lib $FPIC"

# paths configuration
WXWIDGETS_BASENAME="wxWidgets"
WXWIDGETS_URL="https://github.com/pkulchenko/wxWidgets.git"

WXLUA_BASENAME="wxlua"
WXLUA_URL="https://github.com/pkulchenko/wxlua.git"

LUASOCKET_BASENAME="luasocket-3.0-rc1"
LUASOCKET_FILENAME="v3.0-rc1.zip"
LUASOCKET_URL="https://github.com/diegonehab/luasocket/archive/$LUASOCKET_FILENAME"

LUASEC_BASENAME="luasec-0.6"
LUASEC_FILENAME="$LUASEC_BASENAME.zip"
LUASEC_URL="https://github.com/brunoos/luasec/archive/$LUASEC_FILENAME"

LFS_BASENAME="v_1_6_3"
LFS_FILENAME="$LFS_BASENAME.tar.gz"
LFS_URL="https://github.com/keplerproject/luafilesystem/archive/$LFS_FILENAME"

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
  luasec)
    BUILD_LUASEC=true
    ;;
  luasocket)
    BUILD_LUASOCKET=true
    ;;
  lfs)
    BUILD_LFS=true
    ;;
  debug)
    WXWIDGETSDEBUG="--enable-debug=max --enable-debug_gdb"
    WXLUABUILD="Debug"
    ;;
  all)
    BUILD_WXWIDGETS=true
    BUILD_LUA=true
    BUILD_WXLUA=true
    BUILD_LUASOCKET=true
    ;;
  *)
    echo "Error: invalid argument $ARG"
    exit 1
    ;;
  esac
done

# check for g++
if [ ! "$(which g++)" ]; then
  echo "Error: g++ isn't found. Please install GNU C++ compiler."
  exit 1
fi

# check for cmake
if [ ! "$(which cmake)" ]; then
  echo "Error: cmake isn't found. Please install CMake and add it to PATH."
  exit 1
fi

# check for git
if [ ! "$(which git)" ]; then
  echo "Error: git isn't found. Please install console GIT client."
  exit 1
fi

# check for wget
if [ ! "$(which wget)" ]; then
  echo "Error: wget isn't found. Please install GNU Wget."
  exit 1
fi

# create the installation directory
mkdir -p "$INSTALL_DIR" || { echo "Error: cannot create directory $INSTALL_DIR"; exit 1; }

LUAV="51"
LUAS=""
LUA_BASENAME="lua-5.1.5"

if [ $BUILD_52 ]; then
  LUAV="52"
  LUAS=$LUAV
  LUA_BASENAME="lua-5.2.4"
fi

LUA_FILENAME="$LUA_BASENAME.tar.gz"
LUA_URL="http://www.lua.org/ftp/$LUA_FILENAME"

if [ $BUILD_53 ]; then
  LUAV="53"
  LUAS=$LUAV
  LUA_BASENAME="lua-5.3.1"
  LUA_FILENAME="$LUA_BASENAME.tar.gz"
  LUA_URL="http://www.lua.org/ftp/$LUA_FILENAME"
fi

if [ $BUILD_JIT ]; then
  LUA_BASENAME="luajit"
  LUA_URL="https://github.com/pkulchenko/luajit.git"
fi

# build wxWidgets
if [ $BUILD_WXWIDGETS ]; then
  git clone "$WXWIDGETS_URL" "$WXWIDGETS_BASENAME" || { echo "Error: failed to get wxWidgets"; exit 1; }

  cd "$WXWIDGETS_BASENAME"
  ./configure --prefix="$INSTALL_DIR" $WXWIDGETSDEBUG --disable-shared --enable-unicode \
    --enable-compat28 \
    --with-libjpeg=builtin --with-libpng=builtin --with-libtiff=no --with-expat=no \
    --with-zlib=builtin --disable-richtext --with-gtk=2 \
    CFLAGS="-Os -fPIC" CXXFLAGS="-Os -fPIC"
  make $MAKEFLAGS || { echo "Error: failed to build wxWidgets"; exit 1; }
  make install
  cd ..
  rm -rf "$WXWIDGETS_BASENAME"
fi

# build Lua
if [ $BUILD_LUA ]; then
  if [ $BUILD_JIT ]; then
    git clone "$LUA_URL" "$LUA_BASENAME"
    (cd "$LUA_BASENAME"; git checkout v2.0.4)
  else
    wget -c "$LUA_URL" -O "$LUA_FILENAME" || { echo "Error: failed to download Lua"; exit 1; }
    tar -xzf "$LUA_FILENAME"
  fi
  cd "$LUA_BASENAME"

  if [ $BUILD_JIT ]; then
    make CCOPT="-DLUAJIT_ENABLE_LUA52COMPAT" || { echo "Error: failed to build Lua"; exit 1; }
    make install PREFIX="$INSTALL_DIR"
    cp "$INSTALL_DIR"/bin/luajit "$INSTALL_DIR/bin/lua"
    # don't copy luajit includes as the libraries should be compiled using Lua headers
  else
    # use POSIX as it has minimum dependencies (no readline and no ncurses required)
    # LUA_USE_DLOPEN is required for loading libraries
    (cd src; make all MYCFLAGS="$FPIC -DLUA_USE_POSIX -DLUA_USE_DLOPEN" MYLIBS="-Wl,-E -ldl") || { echo "Error: failed to build Lua"; exit 1; }
    make install INSTALL_TOP="$INSTALL_DIR"
  fi
  cp "$INSTALL_DIR/bin/lua" "$INSTALL_DIR/bin/lua$LUAV"

  cd ..
  rm -rf "$LUA_FILENAME" "$LUA_BASENAME"
fi

# build wxLua
if [ $BUILD_WXLUA ]; then
  git clone "$WXLUA_URL" "$WXLUA_BASENAME" || { echo "Error: failed to get wxWidgets"; exit 1; }
  cd "$WXLUA_BASENAME/wxLua"
  git checkout wxwidgets311

  # the following patches wxlua source to fix live coding support in wxlua apps
  # http://www.mail-archive.com/wxlua-users@lists.sourceforge.net/msg03225.html
  sed -i 's/\(m_wxlState = wxLuaState(wxlState.GetLuaState(), wxLUASTATE_GETSTATE|wxLUASTATE_ROOTSTATE);\)/\/\/ removed by ZBS build process \/\/ \1/' modules/wxlua/wxlcallb.cpp

  # remove "Unable to call an unknown method..." error as it leads to a leak
  # see http://sourceforge.net/p/wxlua/mailman/message/34629522/ for details
  sed -i '/Unable to call an unknown method/{N;s/.*/    \/\/ removed by ZBS build process/}' modules/wxlua/wxlbind.cpp

  cmake -G "Unix Makefiles" -DCMAKE_INSTALL_PREFIX="$INSTALL_DIR" -DCMAKE_BUILD_TYPE=$WXLUABUILD -DBUILD_SHARED_LIBS=FALSE \
    -DwxWidgets_CONFIG_EXECUTABLE="$INSTALL_DIR/bin/wx-config" \
    -DwxWidgets_COMPONENTS="stc;gl;html;aui;adv;core;net;base" \
    -DwxLuaBind_COMPONENTS="stc;gl;html;aui;adv;core;net;base" -DwxLua_LUA_LIBRARY_USE_BUILTIN=FALSE \
    -DwxLua_LUA_INCLUDE_DIR="$INSTALL_DIR/include" -DwxLua_LUA_LIBRARY="$INSTALL_DIR/lib/liblua.a" .
  (cd modules/luamodule; make $MAKEFLAGS) || { echo "Error: failed to build wxLua"; exit 1; }
  (cd modules/luamodule; make install)
  [ -f "$INSTALL_DIR/lib/libwx.so" ] || { echo "Error: libwx.so isn't found"; exit 1; }
  [ "$WXLUABUILD" != "Debug" ] && strip --strip-unneeded "$INSTALL_DIR/lib/libwx.so"
  cd ../..
  rm -rf "$WXLUA_BASENAME"
fi

# build LuaSocket
if [ $BUILD_LUASOCKET ]; then
  wget --no-check-certificate -c "$LUASOCKET_URL" -O "$LUASOCKET_FILENAME" || { echo "Error: failed to download LuaSocket"; exit 1; }
  unzip "$LUASOCKET_FILENAME"
  cd "$LUASOCKET_BASENAME"
  mkdir -p "$INSTALL_DIR/lib/lua/$LUAV/"{mime,socket}
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/$LUAV/mime/core.so" src/mime.c -llua \
    || { echo "Error: failed to build LuaSocket"; exit 1; }
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/$LUAV/socket/core.so" \
    src/{auxiliar.c,buffer.c,except.c,inet.c,io.c,luasocket.c,options.c,select.c,tcp.c,timeout.c,udp.c,usocket.c} -llua \
    || { echo "Error: failed to build LuaSocket"; exit 1; }
  mkdir -p "$INSTALL_DIR/share/lua/$LUAV/socket"
  cp src/{ftp.lua,http.lua,smtp.lua,tp.lua,url.lua} "$INSTALL_DIR/share/lua/$LUAV/socket"
  cp src/{ltn12.lua,mime.lua,socket.lua} "$INSTALL_DIR/share/lua/$LUAV"
  [ -f "$INSTALL_DIR/lib/lua/$LUAV/mime/core.so" ] || { echo "Error: mime/core.so isn't found"; exit 1; }
  [ -f "$INSTALL_DIR/lib/lua/$LUAV/socket/core.so" ] || { echo "Error: socket/core.so isn't found"; exit 1; }
  cd ..
  rm -rf "$LUASOCKET_FILENAME" "$LUASOCKET_BASENAME"
fi

# build lfs
if [ $BUILD_LFS ]; then
  wget --no-check-certificate -c "$LFS_URL" -O "$LFS_FILENAME" || { echo "Error: failed to download lfs"; exit 1; }
  tar -xzf "$LFS_FILENAME"
  mv "luafilesystem-$LFS_BASENAME" "$LFS_BASENAME"
  cd "$LFS_BASENAME/src"
  mkdir -p "$INSTALL_DIR/lib/lua/$LUAD/"
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/$LUAD/lfs.so" lfs.c \
    || { echo "Error: failed to build lfs"; exit 1; }
  [ -f "$INSTALL_DIR/lib/lua/$LUAD/lfs.so" ] || { echo "Error: lfs.so isn't found"; exit 1; }
  cd ../..
  rm -rf "$LFS_FILENAME" "$LFS_BASENAME"
fi

# build LuaSec
if [ $BUILD_LUASEC ]; then
  # build LuaSec
  wget --no-check-certificate -c "$LUASEC_URL" -O "$LUASEC_FILENAME" || { echo "Error: failed to download LuaSec"; exit 1; }
  unzip "$LUASEC_FILENAME"
  # the folder in the archive is "luasec-luasec-....", so need to fix
  mv "luasec-$LUASEC_BASENAME" $LUASEC_BASENAME
  cd "$LUASEC_BASENAME"
  gcc $BUILD_FLAGS -o "$INSTALL_DIR/lib/lua/$LUAD/ssl.so" \
    src/luasocket/{timeout.c,buffer.c,io.c,usocket.c} src/{context.c,x509.c,ssl.c} -Isrc \
    -lssl -lcrypto \
    || { echo "Error: failed to build LuaSec"; exit 1; }
  cp src/ssl.lua "$INSTALL_DIR/share/lua/$LUAD"
  mkdir -p "$INSTALL_DIR/share/lua/$LUAD/ssl"
  cp src/https.lua "$INSTALL_DIR/share/lua/$LUAD/ssl"
  [ -f "$INSTALL_DIR/lib/lua/$LUAD/ssl.so" ] || { echo "Error: ssl.so isn't found"; exit 1; }
  strip --strip-unneeded "$INSTALL_DIR/lib/lua/$LUAD/ssl.so"
  cd ..
  rm -rf "$LUASEC_FILENAME" "$LUASEC_BASENAME"
fi

# now copy the compiled dependencies to ZBS binary directory
mkdir -p "$BIN_DIR" || { echo "Error: cannot create directory $BIN_DIR"; exit 1; }
[ $BUILD_LUA ] && cp "$INSTALL_DIR/bin/lua$LUAS" "$BIN_DIR"
[ $BUILD_WXLUA ] && cp "$INSTALL_DIR/lib/libwx.so" "$BIN_DIR/clibs"
[ $BUILD_LFS ] && cp "$INSTALL_DIR/lib/lua/$LUAD/lfs.so" "$BIN_DIR/clibs$LUAS"

if [ $BUILD_LUASOCKET ]; then
  mkdir -p "$BIN_DIR/clibs$LUAS/"{mime,socket}
  cp "$INSTALL_DIR/lib/lua/$LUAV/mime/core.so" "$BIN_DIR/clibs$LUAS/mime"
  cp "$INSTALL_DIR/lib/lua/$LUAV/socket/core.so" "$BIN_DIR/clibs$LUAS/socket"
fi

if [ $BUILD_LUASEC ]; then
  cp "$INSTALL_DIR/lib/lua/$LUAD/ssl.so" "$BIN_DIR/clibs$LUAS"
  cp "$INSTALL_DIR/share/lua/$LUAD/ssl.lua" ../lualibs
  cp "$INSTALL_DIR/share/lua/$LUAD/ssl/https.lua" ../lualibs/ssl
fi

echo "*** Build has been successfully completed ***"
exit 0
