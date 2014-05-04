#!/bin/sh

zenity --error --title "OpenRA" --text "OpenRA has encountered a fatal error."

zenity --question --title "OpenRA" --text="Would you like have a look at the crash log files?" || exit

xdg-open ~/.openra/Logs

zenity --question --title "OpenRA" --text="Would you like to read the FAQ?" || exit

xdg-open https://github.com/OpenRA/OpenRA/wiki/FAQ