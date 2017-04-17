#!/bin/bash

case "$1" in
	bleed)
		exit
		;;
	next | playtest-*)
		TAG=" (playtest)"
		;;
	master | release-*)
		TAG=""
		;;
	*)
		echo "Unknown branch: $1"
		exit
		;;
esac

echo "Updating https://github.com/OpenRA/OpenRA/wiki/"

SSH_KEY="$HOME"/.ssh/id_rsa

mkdir -p "$(dirname "$SSH_KEY")"
openssl aes-256-cbc -k "$KEY" -in ssh.enc -d -out "$SSH_KEY"
chmod 0600 "$SSH_KEY"

rm -rf $HOME/openra-wiki
git clone git@github.com:OpenRA/OpenRA.wiki.git $HOME/openra-wiki
cp -fr ../DOCUMENTATION.md "${HOME}/openra-wiki/Traits${TAG}.md"
cp -fr ../Lua-API.md "${HOME}/openra-wiki/Lua API${TAG}.md"

pushd $HOME/openra-wiki
git config --local user.email "orabot@users.noreply.github.com"
git config --local user.name "orabot"
git add "Traits${TAG}.md"
git add "Lua API${TAG}.md"
git commit -m "Update trait and scripting documentation for branch $1" &&
git push origin master
popd

shred -u "$SSH_KEY"
