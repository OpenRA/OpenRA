#!/bin/sh
# Note: this relies on the non-standard -f flag implemented by gnu readlink
mono OpenRA.Game.exe Engine.LaunchPath="$(readlink -f $0)" "$@"
if [ $? != 0 -a $? != 1 ]
then
	ZENITY=`which zenity` || echo "OpenRA needs zenity installed to display a graphical error dialog. See ~/.openra. for log files."
	$ZENITY --question --title "OpenRA" --text "OpenRA has encountered a fatal error.\nLog Files are available in ~/.openra." --ok-label "Quit" --cancel-label "View FAQ" || xdg-open https://github.com/OpenRA/OpenRA/wiki/FAQ
	exit 1
fi
