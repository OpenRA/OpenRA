#!/bin/sh
MODLAUNCHER=$(python -c "import os; print(os.path.realpath('$0'))")

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
mono OpenRA.Game.exe Engine.LaunchPath="$MODLAUNCHER" $MODARG "$@"

# Show a crash dialog if something went wrong
if [ $? != 0 ] && [ $? != 1 ]; then
	ERROR_MESSAGE="OpenRA has encountered a fatal error.\nPlease refer to the crash logs and FAQ for more information.\n\nLog files are located in ~/.openra/Logs\nThe FAQ is available at http://wiki.openra.net/FAQ"
	if command -v zenity > /dev/null; then
		zenity --no-wrap --error --title "{MODNAME}" --text "${ERROR_MESSAGE}" 2> /dev/null
	elif command -v kdialog > /dev/null; then
		kdialog --title "{MODNAME}" --error "${ERROR_MESSAGE}"
	else
		printf "%s\n" "${ERROR_MESSAGE}"
	fi
	exit 1
fi
