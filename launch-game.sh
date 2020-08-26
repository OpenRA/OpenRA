#!/bin/sh
if command -v python3 >/dev/null 2>&1; then
	 MODLAUNCHER=$(python3 -c "import os; print(os.path.realpath('$0'))")
else
	 MODLAUNCHER=$(python -c "import os; print(os.path.realpath('$0'))")
fi

# Prompt for a mod to launch if one is not already specified
MODARG=''
if [ z"${*#*Game.Mod}" = z"$*" ]
then
	if command -v zenity > /dev/null
	then
		TITLE=$(zenity --forms --add-combo="" --combo-values="Red Alert|Tiberian Dawn|Dune 2000|Tiberian Sun" --text "Select mod" --title="" || echo "cancel")
		if [ "$TITLE" = "cancel" ]; then exit 0
		elif [ "$TITLE" = "Tiberian Dawn" ]; then MODARG='Game.Mod=cnc'
		elif [ "$TITLE" = "Dune 2000" ]; then MODARG='Game.Mod=d2k'
		elif [ "$TITLE" = "Tiberian Sun" ]; then MODARG='Game.Mod=ts'
		else MODARG='Game.Mod=ra'
		fi
	else
		echo "Please provide the Game.Mod=\$MOD argument (possible \$MOD values: ra, cnc, d2k, ts)"
		exit 1
	fi
fi

# Launch the engine with the appropriate arguments
mono --debug bin/OpenRA.dll Engine.EngineDir=".." Engine.LaunchPath="$MODLAUNCHER" $MODARG "$@"

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
	ERROR_MESSAGE="OpenRA has encountered a fatal error.\nPlease refer to the crash logs and FAQ for more information.\n\nLog files are located in ${LOGS}\nThe FAQ is available at http://wiki.openra.net/FAQ"
	if command -v zenity > /dev/null; then
		zenity --no-wrap --error --title "OpenRA" --text "${ERROR_MESSAGE}" 2> /dev/null
	elif command -v kdialog > /dev/null; then
		kdialog --title "OpenRA" --error "${ERROR_MESSAGE}"
	else
		echo "${ERROR_MESSAGE}"
	fi
	exit 1
fi
