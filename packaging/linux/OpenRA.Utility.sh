#!/bin/bash
# OpenRA utility wrapper for linux systems

authenticate () {
    if command -v gksudo > /dev/null; then
        gksudo --description "OpenRA Installer" -- mono OpenRA.Utility.exe $@ || echo "Error: Permission denied."
    elif command -v kdesudo > /dev/null; then
        kdesudo -d -- mono OpenRA.Utility.exe $@ || echo "Error: Permission denied."
    else
        # Try running without escalating
        mono OpenRA.Utility.exe $@
    fi
}

if [[ "$1" == "--display-filepicker" ]]; then
    if command -v zenity > /dev/null ; then
        zenity --file-selection --title $2
    else
        mono OpenRA.Utility.exe $@
    fi
else
    # Everything else requires root running an -inner variant
    authenticate "$1-inner" ${@:2}
fi