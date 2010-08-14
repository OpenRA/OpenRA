#!/bin/bash

TAG=$1

VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`

_gitroot="git://github.com/chrisforbes/OpenRA.git"
_gitname="OpenRA"

mkdir ~/openra-package/
pushd ~/openra-package/

echo "Connecting to GIT server...."

if [ -d $_gitname ] ; then
  pushd $_gitname && git pull origin
  echo "The local files are updated."
  popd
else
  git clone $_gitroot $_gitname
fi

echo "GIT checkout done or server timeout"
echo "Starting make..."

rm -rf "$_gitname-build"
git clone "$_gitname" "$_gitname-build"
pushd "$_gitname-build"
git checkout $TAG

make prefix=/usr all
make prefix=/usr DESTDIR=../built install
popd
popd

pushd linux/pkgbuild/
sh buildpackage.sh "ftp.open-ra.org" "httpdocs/releases/linux" "$2" "$3" "$VERSION"
popd

pushd linux/rpm/
sh buildpackage.sh "ftp.open-ra.org" "httpdocs/releases/linux" "$2" "$3" "$VERSION" "~/rpmbuild"
popd

pushd osx/
#todo
popd
