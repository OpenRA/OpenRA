#!/bin/sh
# OpenRA Packaging script for osx
#   Creates a .app bundle for OpenRA
#   Patches and packages mono to work from within the app bundle

# A list of the binaries that may contain references to dependencies in the gac
DEPS_LOCAL="OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll"

# A list of external dependencies, (aside from mono)
# Note: references to frameworks are currently hardcoded in the .config hacking step
DEPS_FRAMEWORKS="/Library/Frameworks/Cg.framework /Library/Frameworks/SDL.framework"

PACKAGING_DIR="osxbuild"
SYSTEM_MONO="/Library/Frameworks/Mono.framework/Versions/2.6.3"
LOCAL_MONO="$PACKAGING_DIR/OpenRA.app/Contents/Frameworks/Mono.framework/Versions/2.6.3"



# Todo: make this go away when we kill the gac stuff
# dylibs referred to by dlls in the gac; won't show up to otool
GAC_DYLIBS="$SYSTEM_MONO/lib/libMonoPosixHelper.dylib $SYSTEM_MONO/lib/libgdiplus.dylib "



mkdir -p $PACKAGING_DIR/OpenRA.app/
cp -r ./packaging/osx/OpenRA.app/* $PACKAGING_DIR/OpenRA.app/

function patch_mono {
	echo "Patching: "$1
	LIBS=$( otool -L $1 | grep /Library/Frameworks/Mono.framework/ | awk {'print $1'} )
	for i in $LIBS; do
		install_name_tool -change $i @executable_path/../${i:9} $1
	done
	for i in $LIBS; do
		if [ ! -e $PACKAGING_DIR/OpenRA.app/Contents/${i:9} ]; then
			mkdir -p $PACKAGING_DIR/OpenRA.app/Contents/`dirname ${i:9}`
			cp $i $PACKAGING_DIR/OpenRA.app/Contents/`dirname ${i:9}`
			patch_mono $PACKAGING_DIR/OpenRA.app/Contents/${i:9}
		fi
	done
}

# Setup environment for mkbundle
# Force 32-bit build and set the pkg-config path for mono.pc
export AS="as -arch i386"
export CC="gcc -arch i386 -mmacosx-version-min=10.5 -isysroot /Developer/SDKs/MacOSX10.5.sdk"
export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig/
export PATH=/sw/bin:/sw/sbin:$PATH

# Copy and patch mono
echo "Copying and patching mono..."

mkdir -p "$LOCAL_MONO/bin/"
cp "$SYSTEM_MONO/bin/mono" "$PACKAGING_DIR/OpenRA.app/Contents/MacOS/"
patch_mono "$PACKAGING_DIR/OpenRA.app/Contents/MacOS/mono"

# Copy the gac dylibs into the app bundle
for i in $GAC_DYLIBS; do
	mkdir -p $PACKAGING_DIR/OpenRA.app/Contents/`dirname ${i:9}`
	cp $i $PACKAGING_DIR/OpenRA.app/Contents/`dirname ${i:9}`
	patch_mono $PACKAGING_DIR/OpenRA.app/Contents/${i:9}
done


# Find the dlls that are used by the game; copy them into the app bundle and patch/package any dependencies
echo "Determining dlls used by the game..."

DLL_DIR="$LOCAL_MONO/lib"

# This is a huge hack, but reliably gets us the dlls to include
DLLS=`mkbundle --deps --static -z -c -o OpenRA $DEPS_LOCAL | grep "embedding: "`
for i in $DLLS; do
  	if [ "$i" != "embedding:" ]; then
		cp $i $DLL_DIR
		if [ -e "$i.config" ]; then
			CONFIG="$DLL_DIR/`basename $i`.config"
			echo "Patching config `basename $CONFIG`"
			# Remove any references to the hardcoded mono framework; the game will look in the right location anyway
			#sed "s/\/Library\/Frameworks\/Mono.framework\/Versions\/2.6.3\/lib\///" "$i.config" > "${CONFIG}_1"
			sed "s/\/Library\/Frameworks\/Mono.framework/..\/Mono.framework/" "$i.config" > "${CONFIG}"
#			sed "s/\/Library\/Frameworks\/Cg.framework/..\/Cg.framework/" "${CONFIG}_1" > "${CONFIG}_2"
#			sed "s/\/Library\/Frameworks\/SDL.framework/..\/SDL.framework/" "${CONFIG}_2" > $CONFIG
#			rm "${CONFIG}_1" "${CONFIG}_2"
		fi
	fi
done


# Remove the files themselves that we accidentally copied over
for i in $DEPS_LOCAL; do
	rm "$DLL_DIR/$i"
done

# Copy external frameworks
#echo "Copying external frameworks..."
#for i in $DEPS_FRAMEWORKS; do
#	cp -RL $i $PACKAGING_DIR/OpenRA.app/Contents/${i:9}
#done

echo "All Done!"
