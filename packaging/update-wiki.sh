echo "Updating https://github.com/OpenRA/OpenRA/wiki/Traits"
rm -rf openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git openra-wiki
cp -fr ../DOCUMENTATION.md openra-wiki/Traits.md
cd openra-wiki
git add Traits.md
git commit -m "Update trait documentation"
git push origin master