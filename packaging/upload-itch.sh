#!/bin/bash

export GIT_TAG="$1"
export BUILD_OUTPUT_DIR="$2"

case "${GIT_TAG}" in
	bleed)
		exit
		;;
	next | playtest-*)
		exit
		;;
	master | release-*)
		;;
	*)
		echo "Unknown branch: $1"
		exit
		;;
esac

if command -v curl >/dev/null 2>&1; then
	curl -L -o butler-linux-amd64.zip https://broth.itch.ovh/butler/linux-amd64/LATEST/archive/default
else
	wget -cq -O butler-linux-amd64.zip https://broth.itch.ovh/butler/linux-amd64/LATEST/archive/default
fi

unzip butler-linux-amd64.zip
chmod +x butler

./butler -V
./butler login
cp ${BUILD_OUTPUT_DIR}/OpenRA-${GIT_TAG}-x64-winportable.zip OpenRA-${GIT_TAG}-x64-win-itch-portable.zip
zip -u OpenRA-${GIT_TAG}-x64-win-itch-portable.zip .itch.toml
./butler push "OpenRA-${GIT_TAG}-x64-win-itch-portable.zip" "openra/openra:win" --userversion-file ../VERSION
./butler push --fix-permissions "${BUILD_OUTPUT_DIR}/OpenRA-${GIT_TAG}.dmg" "openra/openra:osx" --userversion-file ../VERSION
./butler push --fix-permissions "${BUILD_OUTPUT_DIR}/OpenRA-Dune-2000-x86_64.AppImage" "openra/openra:linux-d2k" --userversion-file ../VERSION
./butler push --fix-permissions "${BUILD_OUTPUT_DIR}/OpenRA-Red-Alert-x86_64.AppImage" "openra/openra:linux-ra" --userversion-file ../VERSION
./butler push --fix-permissions "${BUILD_OUTPUT_DIR}/OpenRA-Tiberian-Dawn-x86_64.AppImage" "openra/openra:linux-td" --userversion-file ../VERSION

rm butler butler-linux-amd64.zip 7z.so libc7zip.so
