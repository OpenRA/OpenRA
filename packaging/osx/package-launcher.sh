#!/bin/sh
# OpenRA Packaging script for osx
#    Creates a self contained app bundle that contains
#    all the dependencies required to run the game.
#       This script assumes that it is being run on osx >= 10.5
#    and that all the required dependencies are installed
#    and the dependant dlls exist in the system GAC.
#       $GAME_PATH in OpenRA.app/Contents/MacOS/OpenRA
#    specifies the game directory to load. This can point
#    anywhere on the filesystem.

# A list of the binaries that we want to be able to run
DEPS_LOCAL="OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll"
PACKAGING_DIR="osxbuild/launcher"
SYSTEM_MONO="/Library/Frameworks/Mono.framework/Versions/2.6.3"

# dylibs referred to by dlls in the gac; won't show up to otool
GAC_DYLIBS="$SYSTEM_MONO/lib/libMonoPosixHelper.dylib $SYSTEM_MONO/lib/libgdiplus.dylib "

####################################################################################
EXE_DIR="$PACKAGING_DIR/OpenRA.app/Contents/MacOS"
LIB_DIR="$EXE_DIR/lib"

function patch_mono {
	echo "Patching binary: "$1
	LIBS=$( otool -L $1 | grep /Library/Frameworks/Mono.framework/ | awk {'print $1'} )
	# Todo: fix the -id?
	for i in $LIBS; do
		install_name_tool -change $i @executable_path/lib/`basename $i` $1
	done
	
	# If it still matches then we also need to change the id
	LIBS2=$( otool -L $1 | grep /Library/Frameworks/Mono.framework/ | awk {'print $1'} )
	for i in $LIBS2; do
		install_name_tool -id @executable_path/lib/`basename $i` $1
	done
	
	for i in $LIBS; do
		FILE=`basename $i`
		if [ ! -e $LIB_DIR/$FILE ]; then
			cp $i $LIB_DIR
			patch_mono $LIB_DIR/$FILE
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
echo "Creating app bundle..."


# Create the directory tree and copy in our existing files
mkdir -p $PACKAGING_DIR/OpenRA.app
cp -r ./packaging/osx/OpenRA.app/* $PACKAGING_DIR/OpenRA.app/
mkdir -p $LIB_DIR


cp "$SYSTEM_MONO/bin/mono" $EXE_DIR
patch_mono "$EXE_DIR/mono"

# Copy the gac dylibs into the app bundle
for i in $GAC_DYLIBS; do
	cp $i $LIB_DIR
	patch_mono $LIB_DIR/`basename $i`
done


# Find the dlls that are used by the game; copy them into the app bundle and patch/package any dependencies
echo "Searching for dlls..."

# This is a huge hack, but it works
DLLS=`mkbundle --deps --static -z -c -o OpenRA $DEPS_LOCAL | grep "embedding: "`
for i in $DLLS; do
  	if [ "$i" != "embedding:" ]; then
		cp $i $LIB_DIR
		if [ -e "$i.config" ]; then
			CONFIG=$LIB_DIR/"`basename $i`.config"
			
			echo "Patching config: $CONFIG"
			# Remove any references to the hardcoded mono framework; the game will look in the right location anyway
			sed "s/\/Library\/Frameworks\/Mono.framework\/Versions\/2.6.3\///" "$i.config" > "${CONFIG}_1"
			sed "s/\/Library\/Frameworks\/Cg.framework/lib/" "${CONFIG}_1" > "${CONFIG}_2"
			sed "s/\/Library\/Frameworks\/SDL.framework/lib/" "${CONFIG}_2" > $CONFIG
			rm "${CONFIG}_1" "${CONFIG}_2"
		fi
	fi
done

# Remove the files that we want to run that we accidentally copied over
for i in $DEPS_LOCAL; do
	rm "$LIB_DIR/$i"
done


# Copy external frameworks
echo "Copying Cg..."
cp /Library/Frameworks/Cg.framework/Cg $LIB_DIR
chmod 755 $LIB_DIR/Cg

echo "Copying SDL..."
cp /Library/Frameworks/SDL.framework/SDL $LIB_DIR
xattr -d com.apple.quarantine $LIB_DIR/SDL
chmod 755 $LIB_DIR/SDL

echo "All Done!"
