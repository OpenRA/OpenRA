#!/bin/bash

case "$1" in
	"bleed")
		exit
		;;
	"next")
		TAG=" (playtest)"
		;;
	"master")
		TAG=""
		;;
	*)
		#Eh? Unknown branch
		exit
		;;
esac

git config --global user.email "orabot@users.noreply.github.com"
git config --global user.name "orabot"

echo "Updating https://github.com/OpenRA/OpenRA/wiki/"
rm -rf $HOME/openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git $HOME/openra-wiki
cp -fr ../DOCUMENTATION.md "${HOME}/openra-wiki/Traits${TAG}.md"
cp -fr ../Lua-API.md "${HOME}/openra-wiki/Lua API${TAG}.md"

pushd $HOME/openra-wiki
git add "Traits${TAG}.md"
git add "Lua API${TAG}.md"
git commit -m "Update trait and scripting documentation for branch $1"
git push origin master
popd
