#!/bin/bash

# Files to include in the package
# Specified relative to the build-dir (argument $1)
FILES="OpenRA.Game.exe OpenRA.Gl.dll OpenRA.FileFormats.dll FreeSans.ttf FreeSansBold.ttf titles.ttf shaders mods/ra mods/cnc VERSION"

# Files that match the above patterns, that should be excluded
EXCLUDE="*.mdb"

if [ $# -ne "2" ]; then
	echo "Usage: `basename $0` build-dir version"
    exit 1
fi

# Dirty build dir; last build failed?
if [ -e "OpenRA.app" ]; then
	echo "Error: OpenRA.app already exists"
    exit 2
fi

# Copy the template to build the game package
# Assumes it is layed out with the correct directory structure
cp -rv template.app OpenRA.app

for i in $FILES; do
	cp -Rv "$1$i" "OpenRA.app/Contents/Resources/$i" || exit 3
done

# Delete excluded files
pushd "OpenRA.app/Contents/Resources/" &> /dev/null
for i in $EXCLUDE; do
	find . -path "$i" -delete
done
popd &> /dev/null

# Package app bundle into a zip
zip OpenRA-$2 -r -9 OpenRA.app
rm -rf OpenRA.app
echo "Done!"