#!/bin/sh
# OpenRA Packaging script for osx
#   Creates a .app bundle for OpenRA game, and a command line app for OpenRa server
#   Statically links all custom dlls into the executable, but requires the Mono and
#   Cg frameworks installed on the target machine for these binaries to run

# List of game files to copy into the app bundle
# TODO: This will be significantly shorter once we move the ra files into its mod dir
GAME_FILES="OpenRA shaders mods maps packaging/osx/settings.ini FreeSans.ttf FreeSansBold.ttf"

# List of system files to copy into the app bundle
# TODO: Sort out whats going on with libglfw so we don't need to do this
SYSTEM_FILES=libglfw.dylib

# Force 32-bit build and set the pkg-config path for mono.pc
export AS="as -arch i386"
export CC="gcc -arch i386"
export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig/

# Package the server binary
mkbundle --deps --static -z -o openra_server OpenRA.Server.exe OpenRa.FileFormats.dll

# Package the game binary
mkbundle --deps --static -z -o OpenRA OpenRa.Game.exe OpenRa.FileFormats.dll thirdparty/Tao/Tao.Glfw.dll thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.OpenAl.dll OpenRa.Gl.dll

# Copy everything into our game bundle template
cp -R packaging/osx/OpenRA.app .
cp -R $GAME_FILES $SYSTEM_FILES OpenRA.app/Contents/Resources/
