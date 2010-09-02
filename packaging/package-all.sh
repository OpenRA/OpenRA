#!/bin/bash

TAG=$1

VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`

_gitroot="git://github.com/chrisforbes/OpenRA.git"
_gitname="OpenRA"

msg () {
  echo -ne $1
  echo $2
  echo -ne "\E[0m"
}

if [ -z $VERSION ]; then
  msg "\E[31m" "Malformed tag $TAG"
  exit 1
fi

if [ ! -d ~/openra-package/ ]; then
  mkdir ~/openra-package/
fi
pushd ~/openra-package/ &> /dev/null

msg "\E[32m" "Connecting to GIT server...."

if [ -d $_gitname ] ; then
  pushd $_gitname &> /dev/null && git pull origin
  msg "\E[32m" "The local files are updated."
  popd &> /dev/null # $_gitname
else
  git clone $_gitroot $_gitname
fi

msg "\E[32m" "GIT checkout done or server timeout"

rm -rf "$_gitname-build"
git clone "$_gitname" "$_gitname-build"
pushd "$_gitname-build" &> /dev/null

msg "\E[32m" "Checking out $TAG"
git checkout $TAG &> /dev/null
if [ $? -ne 0 ]; then
  msg "\E[31m" "Checkout of $TAG failed."
  exit 1
fi

msg "\E[32m" "Starting make..."
make prefix=/usr DESTDIR=../built install
if [ $? -ne 0 ]; then
  msg "\E[31m" "Build failed."
  exit 1
fi

pushd packaging &> /dev/null

#Arch-Linux
msg "\E[34m" "Building Arch-Linux package."
pushd linux/pkgbuild/ &> /dev/null
sh buildpackage.sh "ftp.open-ra.org" "httpdocs/releases/linux" "$2" "$3" "$VERSION" &> package.log
if [ $? -ne 0 ]; then
  msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

#RPM
msg "\E[34m" "Building RPM package."
pushd linux/rpm/ &> /dev/null
sh buildpackage.sh "ftp.open-ra.org" "httpdocs/releases/linux" "$2" "$3" "$VERSION" ~/rpmbuild &> package.log
if [ $? -ne 0 ]; then
  msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

#OSX
msg "\E[34m" "Building OSX package."
pushd osx/ &>/dev/null
sh package-game.sh ~/openra-package/$_gitname-build "$VERSION" &> package.log
if [ $? -eq 0 ]; then
  ../uploader.sh mac "$VERSION" ~/openra-package/$_gitname-build/osxbuild/OpenRA-$VERSION.zip "$2" "$3"
else
  msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

#Windows
msg "\E[34m" "Building Windows package."
pushd windows/ &> /dev/null
makensis -DSRCDIR=/home/openra/openra-package/$_gitname-build OpenRA.nsi &> package.log
if [ $? -eq 0 ]; then
  mv OpenRA.exe OpenRA-$VERSION.exe
  ../uploader.sh windows "$VERSION" OpenRA-$VERSION.exe "$2" "$3"
else
  msg "\E[31m" "Package build failed, refer to log."  
fi
popd &> /dev/null

popd &> /dev/null # packaging
popd &> /dev/null # $_gitname-build
popd &> /dev/null # ~/openra-package/
