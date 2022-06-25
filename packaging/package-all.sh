#!/bin/bash
# OpenRA master packaging script

set -o errexit -o pipefail || exit $?

if [ $# -ne "2" ]; then
	echo "Usage: ${0##*/} version outputdir."
	exit 1
fi

export GIT_TAG="$1"
export BUILD_OUTPUT_DIR="$2"

# Set the working dir to the location of this script using bash parameter expansion
cd "${0%/*}"

#build packages using a subshell so directory changes do not persist beyond the function
function build_package() (
	function on_build() {
		echo "$1 package build failed." 1>&2
	}
	#trap function executes on any error in the following commands
	trap "on_build $1" ERR
	echo "Building $1 package(s)."
	cd "$1"
	./buildpackage.sh "${GIT_TAG}" "${BUILD_OUTPUT_DIR}"
)

if [[ "$OSTYPE" == "darwin"* ]]; then
  build_package macos
else
  build_package windows
  build_package linux
  build_package source
fi

echo "Package build done."
