#!/bin/bash

if [[ $(uname) == 'Darwin' ]]; then
  open zbstudio/ZeroBraneStudio.app --args "$@"
else
  type lua 2>/dev/null >&2 && lua -e "os.exit(pcall(require, 'wx') and 0 or 1)"
  if [[ "$?" != "0" ]]; then (cd build; bash install-deb.sh); fi
  lua src/main.lua zbstudio "$@"
fi
