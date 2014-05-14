#!/bin/sh
ZENITY=`which zenity` || echo "OpenRA needs zenity installed to display a graphical error dialog. See ~/.openra. for log files."
$ZENITY --question --title "OpenRA" --text "OpenRA has encountered a fatal error.\nLog Files are available in ~/.openra." --ok-label "Quit" --cancel-label "View FAQ" || xdg-open https://github.com/OpenRA/OpenRA/wiki/FAQ