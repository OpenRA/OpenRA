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

TYPE=`echo $TAG | grep -o "^[a-z]\\+"`
VERSION=`echo $TAG | grep -o "[0-9]\\+-\\?[0-9]\\?"`

_gitroot="git://github.com/chrisforbes/OpenRA.git"
_gitname="OpenRA"

if [ -z $VERSION ]; then
    msg "\E[31m" "Malformed tag $TAG"
    exit 1
fi


case "$TYPE" in
    "release") 
        FTPPATH="httpdocs/releases"
        ;;
    "playtest") 
        FTPPATH="httpdocs/playtests"
        ;;
    *)
        msg "\E[31m" "Unrecognized tag prefix $TYPE"
        exit 1
        ;;
esac

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

####### *nix Builds #######
pushd linux &> /dev/null

#Desktop Icons
BUILTDIR=../../../built
mkdir -p $BUILTDIR/usr/share/applications/
sed -i "3,3 d" openra-ra.desktop
sed -i "3,3 i\Version=$VERSION" openra-ra.desktop
sed -i "3,3 d" openra-cnc.desktop
sed -i "3,3 i\Version=$VERSION" openra-cnc.desktop
cp openra-ra.desktop $BUILTDIR/usr/share/applications/
cp openra-cnc.desktop $BUILTDIR/usr/share/applications/

#Menu entries
mkdir -p $BUILTDIR/usr/share/menu/
cp openra-ra $BUILTDIR/usr/share/menu/
cp openra-cnc $BUILTDIR/usr/share/menu/

#Icon images
mkdir -p $BUILTDIR/usr/share/pixmaps/
cp openra.32.xpm $BUILTDIR/usr/share/pixmaps/
mkdir -p $BUILTDIR/usr/share/icons/
cp -r hicolor $BUILTDIR/usr/share/icons/

popd &> /dev/null

#Arch-Linux
msg "\E[34m" "Building Arch-Linux package."
pushd linux/pkgbuild/ &> /dev/null
sh buildpackage.sh "ftp.open-ra.org" "$FTPPATH/linux" "$2" "$3" "$VERSION" &> package.log
if [ $? -ne 0 ]; then
    msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

#RPM
msg "\E[34m" "Building RPM package."
pushd linux/rpm/ &> /dev/null
sh buildpackage.sh "ftp.open-ra.org" "$FTPPATH/linux" "$2" "$3" "$VERSION" ~/rpmbuild &> package.log
if [ $? -ne 0 ]; then
    msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

#deb
msg "\E[34m" "Building deb package."
pushd linux/deb/ &> /dev/null
./buildpackage.sh "ftp.open-ra.org" "$FTPPATH/linux" "$2" "$3" "$VERSION" ~/openra-package/built ~/debpackage &> package.log
if [ $? -ne 0 ]; then
  msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

####### OSX #######
msg "\E[34m" "Building OSX package."
pushd osx/ &>/dev/null
sh package-game.sh ~/openra-package/$_gitname-build "$VERSION" &> package.log
if [ $? -eq 0 ]; then
    ../uploader.sh mac "$VERSION" ~/openra-package/$_gitname-build/osxbuild/OpenRA-$VERSION.zip $FTPPATH "$2" "$3"
else
    msg "\E[31m" "Package build failed, refer to log."
fi
popd &> /dev/null

####### Windows #######
msg "\E[34m" "Building Windows package."
pushd windows/ &> /dev/null
makensis -DSRCDIR=/home/openra/openra-package/$_gitname-build OpenRA.nsi &> package.log
if [ $? -eq 0 ]; then
    mv OpenRA.exe OpenRA-$VERSION.exe
    ../uploader.sh windows "$VERSION" OpenRA-$VERSION.exe $FTPPATH "$2" "$3"
else
    msg "\E[31m" "Package build failed, refer to log."  
fi
popd &> /dev/null

popd &> /dev/null # packaging
popd &> /dev/null # $_gitname-build
popd &> /dev/null # ~/openra-package/
