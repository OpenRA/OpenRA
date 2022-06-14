#!/bin/sh
ENGINEDIR=$(dirname "$0")
if command -v mono >/dev/null 2>&1 && [ "$(grep -c .NETCoreApp,Version= ${ENGINEDIR}/bin/OpenRA.dll)" = "0" ]; then
	RUNTIME_LAUNCHER="mono --debug"
else
	RUNTIME_LAUNCHER="dotnet"
fi

if command -v python3 >/dev/null 2>&1; then
	 LAUNCHPATH=$(python3 -c "import os; print(os.path.realpath('$0'))")
else
	 LAUNCHPATH=$(python -c "import os; print(os.path.realpath('$0'))")
fi

# Prompt for a mod to launch if one is not already specified
MODARG=''
if [ z"${*#*Game.Mod=}" = z"$*" ]
then
	if command -v zenity > /dev/null
	then
		TITLE=$(zenity --title='Launch OpenRA' --list --hide-header --text 'Select game mod:' --column 'Game mod' 'Red Alert' 'Tiberian Dawn' 'Dune 2000' 'Tiberian Sun' || echo "cancel")
		if [ "$TITLE" = "Tiberian Dawn" ]; then MODARG='Game.Mod=cnc'
		elif [ "$TITLE" = "Dune 2000" ]; then MODARG='Game.Mod=d2k'
		elif [ "$TITLE" = "Tiberian Sun" ]; then MODARG='Game.Mod=ts'
		elif [ "$TITLE" = "Red Alert" ]; then MODARG='Game.Mod=ra'
		else exit 0
		fi
	else
		echo "Please provide the Game.Mod=\$MOD argument (possible \$MOD values: ra, cnc, d2k, ts)"
		exit 1
	fi
fi

# Launch the engine with the appropriate arguments
${RUNTIME_LAUNCHER} ${ENGINEDIR}/bin/OpenRA.dll Engine.EngineDir=".." Engine.LaunchPath="${LAUNCHPATH}" ${MODARG} "$@"

# Show a crash dialog if something went wrong
if [ $? != 0 ] && [ $? != 1 ]; then
	if [ "$(uname -s)" = "Darwin" ]; then
		LOGS="${HOME}/Library/Application Support/OpenRA/Logs/"
	else
		LOGS="${XDG_CONFIG_HOME:-${HOME}/.config}/openra/Logs"
		if [ ! -d "${LOGS}" ] && [ -d "${HOME}/.openra/Logs" ]; then
			LOGS="${HOME}/.openra/Logs"
		fi
	fi

	test -d Support/Logs && LOGS="${PWD}/Support/Logs"
	ERROR_MESSAGE=$(printf "%s has encountered a fatal error.\nPlease refer to the crash logs and FAQ for more information.\n\nLog files are located in %s\nThe FAQ is available at http://wiki.openra.net/FAQ" "OpenRA" "${LOGS}")
	if command -v zenity > /dev/null; then
		zenity --no-wrap --error --title "OpenRA" --no-markup --text "${ERROR_MESSAGE}" 2> /dev/null
	elif command -v kdialog > /dev/null; then
		kdialog --title "OpenRA" --error "${ERROR_MESSAGE}"
	else
		echo "${ERROR_MESSAGE}"
	fi
	exit 1
fi
