#!/bin/bash
# OpenRA utility wrapper for linux systems with zenity

if [[ "$1" == "--display-filepicker" ]]; then
    if command -v zenity > /dev/null ; then
        zenity --file-selection --title $2
    else
        mono OpenRA.Utility.exe "$@"
    fi
fi