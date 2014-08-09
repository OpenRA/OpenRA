#!/bin/bash

git config --global user.email "travis@travis-ci.org"
git config --global user.name "travis-ci"

echo "Updating https://github.com/OpenRA/OpenRA/wiki/Traits"
rm -rf $HOME/openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git $HOME/openra-wiki
cp -fr ../DOCUMENTATION.md $HOME/openra-wiki/Traits.md

pushd .. &> /dev/null
mono --debug OpenRA.Utility.exe --lua-docs d2k > $HOME/openra-wiki/New-Lua-API.md
popd &> /dev/null

pushd $HOME/openra-wiki
git add Traits.md
git add New-Lua-API.md
git commit -m "Update trait and scripting documentation"
git push origin master
popd