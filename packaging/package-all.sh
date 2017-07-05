#!/bin/bash
# OpenRA master packaging script

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
    set -e
    echo "Building $1 package(s)."
    cd "$1"
    ./buildpackage.sh "${GIT_TAG}" "${BUILD_OUTPUT_DIR}"
)

#exit on any non-zero exited (failed) command
set -e
build_package windows
build_package osx
build_package linux

echo "Package build done."
