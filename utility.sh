#!/bin/sh
set -o errexit || exit $?

ENGINEDIR=$(dirname "$0")
if command -v mono >/dev/null 2>&1 && [ "$(grep -c .NETCoreApp,Version= "${ENGINEDIR}/bin/OpenRA.Utility.dll")" = "0" ]; then
	RUNTIME_LAUNCHER="mono --debug"
else
	RUNTIME_LAUNCHER="dotnet"
fi

ENGINE_DIR=.. ${RUNTIME_LAUNCHER} "${ENGINEDIR}/bin/OpenRA.Utility.dll" "$@"
