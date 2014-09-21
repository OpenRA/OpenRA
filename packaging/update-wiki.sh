#!/bin/bash

git config --global user.email "orabot@users.noreply.github.com"
git config --global user.name "orabot"

echo "Updating https://github.com/OpenRA/OpenRA/wiki/Traits"
rm -rf $HOME/openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git $HOME/openra-wiki
cp -fr ../DOCUMENTATION.md $HOME/openra-wiki/Traits.md
cp -fr ../Lua-API.md $HOME/openra-wiki/New-Lua-API.md

pushd $HOME/openra-wiki
git add Traits.md
git add New-Lua-API.md
git commit -m "Update trait and scripting documentation"
git push origin master
popd