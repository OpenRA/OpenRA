#!/bin/sh

# Set the required environment variables
export AS="as -arch i386"
export CC="gcc -arch i386"
export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig/

# Package a command-line binary for the server
mkbundle --deps -o OpenRA_Server OpenRA.Server.exe OpenRa.FileFormats.dll

# Create the app bundle for the game
macpack -mode:winforms -n OpenRA -r OpenRa.FileFormats.dll,OpenRa.Gl.dll,libglfw.dylib,thirdparty/Tao/Tao.Glfw.dll,thirdparty/Tao/Tao.Cg.dll,thirdparty/Tao/Tao.OpenGl.dll,thirdparty/Tao/Tao.OpenAl.dll,allies.mix,conquer.mix,expand2.mix,general.mix,hires.mix,interior.mix,redalert.mix,russian.mix,snow.mix,sounds.mix,temperat.mix,packaging/osx/settings.ini,line.fx,chrome-shp.fx,chrome-rgba.fx,bogus.sno,bogus.tem,world-shp.fx,tileSet.til,templates.ini,packaging/osx/OpenRa.icns,mods,maps OpenRa.Game.exe

# Package a new binary with included deps
mkbundle --deps -o OpenRA OpenRa.Game.exe OpenRa.Fileformats.dll

# Modify the app bundle with our custom files
cp packaging/osx/Info.plist OpenRA.app/Contents/
rm OpenRA.app/Contents/Resources/OpenRA.exe
cp OpenRA OpenRA.app/Contents/Resources/
cp packaging/osx/OpenRA OpenRA.app/Contents/MacOS/