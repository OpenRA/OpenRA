#!/bin/bash

if [[ $(uname) == 'Darwin' ]]; then
  open zbstudio/ZeroBraneStudio.app --args "$@"
else
  if [[ "$(uname -m)" == "x86_64" ]]; then ARCH="x64"; else ARCH="x86"; fi
  bin/linux/$ARCH/lua src/main.lua zbstudio "$@" &
fi
