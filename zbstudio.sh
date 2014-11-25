#!/bin/bash

# Thanks to StackOverflow wiki (http://stackoverflow.com/a/246128)
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [[ $(uname) == 'Darwin' ]]; then
  (cd "$DIR"; open zbstudio/ZeroBraneStudio.app --args "$@")
else
  case "$(uname -m)" in
	x86_64) ARCH=x64;;
	armv7l) ARCH=armhf;;
	*)	ARCH=x86;;
  esac
  (cd "$DIR"; bin/linux/$ARCH/lua src/main.lua zbstudio "$@") &
fi
