#!/bin/sh
# OpenRA Packaging script for osx
#    Packages all the dependencies required to run the game.
#       This script assumes that it is being run on osx >= 10.5
#    and that all the required dependencies are installed
#    and the dependant dlls exist in the system GAC.

# A list of the binaries that we want to be able to run
DEPS_LOCAL="OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll"
PWD=`pwd`
PACKAGING_PATH="$PWD/osxbuild"
BINARY_PATH="$PACKAGING_PATH/deps"
LIB_PATH="$BINARY_PATH/lib"
MONO_VERSION="2.6.7"
SYSTEM_MONO="/Library/Frameworks/Mono.framework/Versions/"${MONO_VERSION}

# dylibs referred to by dlls in the gac; won't show up to otool
GAC_DYLIBS="$SYSTEM_MONO/lib/libMonoPosixHelper.dylib $SYSTEM_MONO/lib/libgdiplus.dylib "

####################################################################################

function patch_mono {
	echo "Patching binary: "$1
	LIBS=$( otool -L $1 | grep /Library/Frameworks/Mono.framework/ | awk {'print $1'} )
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
		if [ ! -e $LIB_PATH/$FILE ]; then
			cp $i $LIB_PATH
			patch_mono $LIB_PATH/$FILE
		fi
	done
}

# Setup environment for mkbundle
# Force 32-bit build and set the pkg-config path for mono.pc
export AS="as -arch i386"
export CC="gcc -arch i386 -mmacosx-version-min=10.5 -isysroot /Developer/SDKs/MacOSX10.5.sdk"
export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig/
export PATH=/sw/bin:/sw/sbin:$PATH


# Create the directory tree and copy in our existing files
mkdir -p "$LIB_PATH"
cp "$SYSTEM_MONO/bin/mono" "$BINARY_PATH"
patch_mono "$BINARY_PATH/mono"

# Copy the gac dylibs=
for i in $GAC_DYLIBS; do
	cp $i $LIB_PATH
	patch_mono $LIB_PATH/`basename $i`
done

# Find the dlls that are used by the game; copy them into the app bundle and patch/package any dependencies
echo "Searching for dlls... (this will take a while)"

# This is a huge hack, but it works
DLLS=`mkbundle --deps -c -o "$PACKAGING_PATH/bogus" $DEPS_LOCAL | grep "embedding: "`
rm "$PACKAGING_PATH/bogus"
for i in $DLLS; do
  	if [ "$i" != "embedding:" ]; then
		cp $i $LIB_PATH
		if [ -e "$i.config" ]; then
			CONFIG=$LIB_PATH/"`basename $i`.config"
			
			echo "Patching config: $CONFIG"
			# Remove any references to the hardcoded mono framework; the game will look in the right location anyway
			sed "s/\/Library\/Frameworks\/Mono.framework\/Versions\/${MONO_VERSION}\///" "$i.config" > "${CONFIG}_1"
			sed "s/\/Library\/Frameworks\/Cg.framework/lib/" "${CONFIG}_1" > "${CONFIG}_2"
			sed "s/\/Library\/Frameworks\/SDL.framework/lib/" "${CONFIG}_2" > $CONFIG
			rm "${CONFIG}_1" "${CONFIG}_2"
		fi
	fi
done

# Remove the files that we want to run that we accidentally copied over
for i in $DEPS_LOCAL; do
	rm "$LIB_PATH/$i"
done


# Copy external frameworks
echo "Copying Cg..."
cp -X /Library/Frameworks/Cg.framework/Cg $LIB_PATH
chmod 755 $LIB_PATH/Cg

echo "Copying SDL..."
cp -X /Library/Frameworks/SDL.framework/SDL $LIB_PATH
chmod 755 $LIB_PATH/SDL

cd "$BINARY_PATH"
zip osx-deps-v2 -r -9 *
mv osx-deps-v2.zip "$PACKAGING_PATH"
rm -rf "$BINARY_PATH"

echo "All Done!"
