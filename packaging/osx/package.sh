#!/bin/sh
# OpenRA Packaging script for osx
#   Creates a .app bundle for OpenRA game, and a command line app for OpenRa server
#   Statically links all custom dlls into the executable; only requires Mono
#   to run on a non-development machine

# List of game files to copy into the app bundle
# TODO: This will be significantly shorter once we move the ra files into its mod dir
GAME_FILES="OpenRA shaders mods maps packaging/osx/settings.ini FreeSans.ttf FreeSansBold.ttf"

# Force 32-bit build and set the pkg-config path for mono.pc
export AS="as -arch i386"
export CC="gcc -arch i386"
export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig/

# Package the server binary
mkbundle --deps --static -z -o openra_server OpenRA.Server.exe OpenRa.FileFormats.dll

# Package the game binary
mkbundle --deps --static -z -o OpenRA OpenRa.Game.exe OpenRa.Gl.dll OpenRa.FileFormats.dll thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.OpenAl.dll thirdparty/Tao/Tao.FreeType.dll thirdparty/Tao/Tao.Sdl.dll thirdparty/Tao.Externals.dll thirdparty/ISE.FreeType.dll

# Copy game files into our game bundle template
cp -R packaging/osx/OpenRA.app .
cp -R $GAME_FILES OpenRA.app/Contents/Resources/

# Copy frameworks into our game bundle template
mkdir OpenRa.app/Contents/Frameworks/
cp -R /Library/Frameworks/Cg.Framework OpenRa.app/Contents/Frameworks/
cp -R /Library/Frameworks/SDL.Framework OpenRa.app/Contents/Frameworks/