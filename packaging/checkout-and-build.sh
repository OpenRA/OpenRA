#!/bin/bash
ARGS=3
E_BADARGS=85

if [ $# -ne "$ARGS" ]; then
  echo "Usage: `basename $0` tag username password"
  exit $E_BADARGS
fi

msg () {
  echo -ne $1
  echo $2
  echo -ne "\E[0m"
}

TAG=$1

VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`

_gitroot="git://github.com/chrisforbes/OpenRA.git"
_gitname="OpenRA"

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

git describe --tags > "VERSION"

msg "\E[32m" "Starting make..."
make prefix=/usr DESTDIR=../built install
if [ $? -ne 0 ]; then
    msg "\E[31m" "Build failed."
    exit 1
fi

pushd packaging &> /dev/null

./package-all.sh $TAG $2 $3

popd &> /dev/null # packaging
popd &> /dev/null # $_gitname-build
popd &> /dev/null # ~/openra-package/

