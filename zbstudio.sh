#!/bin/bash

if [[ $(uname) == 'Darwin' ]]; then
  open zbstudio/ZeroBraneStudio.app --args "$@"
else
  if [[ "$(uname -m)" == "x86_64" ]]; then ARCH="x64"; else ARCH="x86"; fi
  # Special thanks to StackOverflow wiki (http://stackoverflow.com/a/246128)
  DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
  (cd $DIR; bin/linux/$ARCH/lua src/main.lua zbstudio "$@") &
fi
