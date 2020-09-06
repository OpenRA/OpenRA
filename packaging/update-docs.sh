#!/bin/bash

case "$1" in
	bleed)
		exit
		;;
	next | playtest-*)
		TAG="playtest"
		;;
	master | release-*)
		TAG="release"
		;;
	*)
		echo "Unknown branch: $1"
		exit
		;;
esac

echo "Updating docs.openra.net"

SSH_KEY="$HOME"/.ssh/id_rsa

mkdir -p "$(dirname "$SSH_KEY")"
openssl aes-256-cbc -k "$KEY" -in ssh.enc -d -out "$SSH_KEY"
chmod 0600 "$SSH_KEY"

rm -rf "$HOME/openra-docs"
git clone git@github.com:OpenRA/docs.git "$HOME/openra-docs"

mono --debug ../OpenRA.Utility.exe all --docs "$1" > "${HOME}/openra-docs/api/${TAG}/traits.md"
mono --debug ../OpenRA.Utility.exe all --weapon-docs "$1" > "${HOME}/openra-docs/api/${TAG}/weapons.md"
mono --debug ../OpenRA.Utility.exe all --lua-docs "$1" > "${HOME}/openra-docs/api/${TAG}/lua.md"

pushd "$HOME/openra-docs/api/${TAG}" || exit 1
git config --local user.email "orabot@users.noreply.github.com"
git config --local user.name "orabot"
git add "traits.md"
git add "weapons.md"
git add "lua.md"
git commit -m "Update for branch $1" &&
git push origin master
popd || exit

shred -u "$SSH_KEY"
