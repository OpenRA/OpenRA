#!/bin/bash
# OpenRA packaging script for versioned source tarball

set -o errexit -o pipefail || exit $?

# Set the working dir to the location of this script
HERE=$(dirname "$0")
cd "${HERE}"

TAG="${1:-devtest-19700101}"

if [ -z ${2:-} ]; then
OUTPUTDIR=$(pwd)/../../build/source
mkdir -p $OUTPUTDIR
else
OUTPUTDIR="${2}"
fi

SRCDIR="$(pwd)/../.."

pushd "${SRCDIR}" > /dev/null
make version VERSION="${TAG}"

# The output from `git ls-tree` is too long to fit in a single command (overflows MAX_ARG_STRLEN)
# so `xargs` will automatically split the input across multiple `tar` commands.
# Use the amend flag (r) to prevent each call erasing the output from earlier calls.
rm "${OUTPUTDIR}/OpenRA-${TAG}-source.tar" || :
git ls-tree HEAD --name-only -r -z | xargs -0 tar vrf "${OUTPUTDIR}/OpenRA-${TAG}-source.tar"
bzip2 "${OUTPUTDIR}/OpenRA-${TAG}-source.tar"

make version VERSION='{DEV_VERSION}'

popd > /dev/null
