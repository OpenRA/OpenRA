#!/bin/bash

# ZBS binary directory
BIN_DIR="$(dirname "$PWD")/bin"

# temporary installation directory for dependencies
INSTALL_DIR="$PWD/deps"

# paths to Lua headers/libraries
LUA_INCLUDE_DIR="/usr/include/lua5.1"
LUA_LIBRARY="/usr/lib/x86_64-linux-gnu/liblua5.1.so"

# number of parallel jobs used for building
MAKEFLAGS="-j4"

# paths configuration
WXWIDGETS_BASENAME="wxWidgets"
WXWIDGETS_URL="http://svn.wxwidgets.org/svn/wx/wxWidgets/trunk"

WXLUA_BASENAME="wxlua"
WXLUA_URL="https://wxlua.svn.sourceforge.net/svnroot/wxlua/trunk"

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
  wxlua)
    BUILD_WXLUA=true
    ;;
  all)
    BUILD_WXWIDGETS=true
    BUILD_WXLUA=true
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

# check for svn
if [ ! "$(which svn)" ]; then
  echo "Error: svn isn't found. Please install console SVN client."
  exit 1
fi

# check for wget
if [ ! "$(which wget)" ]; then
  echo "Error: wget isn't found. Please install GNU Wget."
  exit 1
fi

# create the installation directory
mkdir -p "$INSTALL_DIR" || { echo "Error: cannot create directory $INSTALL_DIR"; exit 1; }

# build wxWidgets
if [ $BUILD_WXWIDGETS ]; then
  svn co "$WXWIDGETS_URL" "$WXWIDGETS_BASENAME" || { echo "Error: failed to checkout wxWidgets"; exit 1; }
  cd "$WXWIDGETS_BASENAME"
  ./configure --prefix="$INSTALL_DIR" --disable-debug --disable-shared --enable-unicode \
    --with-libjpeg=builtin --with-libpng=sys --with-libtiff=no --with-expat=no \
    --with-zlib=sys --disable-richtext --with-gtk=2 \
    CFLAGS="-Os -fPIC" CXXFLAGS="-Os -fPIC"
  make $MAKEFLAGS || { echo "Error: failed to build wxWidgets"; exit 1; }
  make install
  cd ..
  rm -rf "$WXWIDGETS_BASENAME"
fi

# build wxLua
if [ $BUILD_WXLUA ]; then
  svn co "$WXLUA_URL" "$WXLUA_BASENAME" || { echo "Error: failed to checkout wxLua"; exit 1; }
  cd "$WXLUA_BASENAME/wxLua"
  # the following patches wxlua source to fix live coding support in wxlua apps
  # http://www.mail-archive.com/wxlua-users@lists.sourceforge.net/msg03225.html
  sed -i 's/\(m_wxlState = wxLuaState(wxlState.GetLuaState(), wxLUASTATE_GETSTATE|wxLUASTATE_ROOTSTATE);\)/\/\/ removed by ZBS build process \/\/ \1/' modules/wxlua/wxlcallb.cpp
  cmake -G "Unix Makefiles" -DBUILD_INSTALL_PREFIX="$INSTALL_DIR" -DCMAKE_BUILD_TYPE=MinSizeRel -DBUILD_SHARED_LIBS=FALSE \
    -DwxWidgets_CONFIG_EXECUTABLE="$INSTALL_DIR/bin/wx-config" \
    -DwxLuaBind_COMPONENTS="stc;html;aui;adv;core;net;base" -DwxLua_LUA_LIBRARY_USE_BUILTIN=FALSE \
    -DwxLua_LUA_INCLUDE_DIR="$LUA_INCLUDE_DIR" -DwxLua_LUA_LIBRARY="$LUA_LIBRARY" .
  make $MAKEFLAGS || { echo "Error: failed to build wxLua"; exit 1; }
  make install/strip
  [ -f "$INSTALL_DIR/lib/libwx.so" ] || { echo "Error: libwx.so isn't found"; exit 1; }
  cd ../..
  rm -rf "$WXLUA_BASENAME"
fi

# now copy the compiled dependencies to ZBS binary directory
mkdir -p "$BIN_DIR" || { echo "Error: cannot create directory $BIN_DIR"; exit 1; }
[ $BUILD_WXLUA ] && cp "$INSTALL_DIR/lib/libwx.so" "$BIN_DIR"

# show a message about successful completion
echo "*** Build has been successfully completed ***"
exit 0
