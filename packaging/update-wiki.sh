echo "Updating https://github.com/OpenRA/OpenRA/wiki/Traits"
rm -rf openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git openra-wiki
cp -fr ../DOCUMENTATION.md openra-wiki/Traits.md

pushd .. &> /dev/null
# d2k depends on all mod libraries
mono --debug OpenRA.Utility.exe --lua-docs d2k > packaging/openra-wiki/New-Lua-API.md
popd &> /dev/null

pushd openra-wiki
git add Traits.md
git add New-Lua-API.md
git commit -m "Update trait and scripting documentation"
git push origin master
popd