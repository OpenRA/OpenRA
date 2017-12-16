#!/bin/bash
# OpenRA packaging script for versioned source tarball

if [ $# -ne "2" ]; then
    echo "Usage: `basename $0` tag outputdir"
    exit 1
fi

# Set the working dir to the location of this script
cd $(dirname $0)

TAG="$1"
OUTPUTDIR="$2"
SRCDIR="$(pwd)/../.."

pushd ${SRCDIR} > /dev/null
make version VERSION="${TAG}"
git ls-tree HEAD --name-only -r -z | xargs -0 tar cvjf "${OUTPUTDIR}/OpenRA-${TAG}-source.tar.bz2"
popd > /dev/null
