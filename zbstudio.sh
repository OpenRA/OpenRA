#!/bin/bash

# Thanks to StackOverflow wiki (http://stackoverflow.com/a/246128)
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [[ $(uname) == 'Darwin' ]]; then
  (cd "$DIR"; open zbstudio/ZeroBraneStudio.app --args "$@")
else
  if [[ "$(uname -m)" == "x86_64" ]]; then ARCH="x64"; else ARCH="x86"; fi
  (cd "$DIR"; bin/linux/$ARCH/lua src/main.lua zbstudio "$@") &
fi
