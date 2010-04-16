#!/bin/sh
# OpenRA Packaging script for osx
#   Creates a .app bundle for OpenRA game, and a command line app for OpenRa server
#   All dependencies are packaged inside the game bundle

# List of game files to copy into the app bundle
GAME_FILES="OpenRA shaders maps FreeSans.ttf FreeSansBold.ttf titles.ttf"

# List of mods to include
MODS="ra cnc"

# Files/directories to include
MODS_INCLUDE_FILES="find mods/$m ! -name \"*.mdb\" ! -name \"packages\""

# dylibs referred to by dlls in the gac; won't show up to otool
GAC_DYLIBS="/Library/Frameworks/Mono.framework/Versions/2.6.3/lib/libMonoPosixHelper.dylib /Library/Frameworks/Mono.framework/Versions/2.6.3/lib/libgdiplus.dylib "

# Remove old app bundle
rm -r OpenRA.app

# Recursively modify and copy the mono files depended on by OpenRA into the app bundle
function patch_mono {
	echo "Patching: "$1
	LIBS=$( otool -L $1 | grep /Library/Frameworks/Mono.framework/ | awk {'print $1'} )
	for i in $LIBS; do
   		if [ "`basename $i`" == "`basename $1`" ]; then
        	install_name_tool -id @executable_path/../${i:9} $1
		else
        	install_name_tool -change  $i @executable_path/../${i:9} $1
		fi
	done
	for i in $LIBS; do
		if [ ! -e OpenRA.app/Contents/${i:9} ]; then
			mkdir -p OpenRA.app/Contents/`dirname ${i:9}`
			cp $i OpenRA.app/Contents/`dirname ${i:9}`
			patch_mono OpenRA.app/Contents/${i:9}
		fi
	done
}

function copy_mods {
    for m in $MODS; do
	mkdir -p "OpenRA.app/Contents/Resources/mods/$m"

	#for f in $( find mods/$m \! -name "*.mdb" \! -name "packages"); do
	#for f in `$MODS_INCLUDE_FILES`; do
	cp -R "mods/$m/" "OpenRA.app/Contents/Resources/mods/$m/"
	#done
    done
}

# Force 32-bit build and set the pkg-config path for mono.pc
export AS="as -arch i386"
export CC="gcc -arch i386 -mmacosx-version-min=10.5 -isysroot /Developer/SDKs/MacOSX10.5.sdk"
export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig/

# Package the game binary
mkbundle --deps --static -z -o OpenRA OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.OpenAl.dll thirdparty/Tao/Tao.FreeType.dll thirdparty/Tao/Tao.Sdl.dll

# Copy game files into our game bundle template
cp -R packaging/osx/OpenRA.app .
cp -R $GAME_FILES OpenRA.app/Contents/Resources/
copy_mods

# Copy frameworks into our game bundle template
mkdir OpenRA.app/Contents/Frameworks/
patch_mono OpenRA.app/Contents/Resources/OpenRA

# The dylibs referenced by dll.configs in the gac don't show up to otool: patch them manually
perl -pi -e 's/\/Library\/Frameworks/..\/Frameworks\/.\/.\/./g' OpenRA.app/Contents/Resources/OpenRA

# Copy the gac dylibs into the app bundle
for i in $GAC_DYLIBS; do
	mkdir -p OpenRA.app/Contents/`dirname ${i:9}`
	cp $i OpenRA.app/Contents/`dirname ${i:9}`
	patch_mono OpenRA.app/Contents/${i:9}
done

cp -R /Library/Frameworks/Cg.framework OpenRA.app/Contents/Frameworks/
cp -R /Library/Frameworks/SDL.framework OpenRA.app/Contents/Frameworks/

# Fix permissions
chmod -R 755 OpenRA.app