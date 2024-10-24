#!/bin/sh
set -o errexit || exit $?

ENGINEDIR=$(dirname "$0")

ENGINE_DIR=.. dotnet "${ENGINEDIR}/bin/OpenRA.Utility.dll" "$@"
