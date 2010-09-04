%define name openra
%define version 20100801.2
Name: %{name}
Version: %{version}
Release: 1
Summary: Open Source rebuild of the Red Alert game engine using Mono/OpenGL.
License: GPL3
URL: http://open-ra.org
Group: Amusements/Games
Packager: Matthew Bowra-Dean <matthew@ijw.co.nz>
Requires: mono-core mono-devel SDL freetype2 openal Mesa cg
Prefix: /usr
Source: %{name}-%{version}.tar.gz
Buildroot: /tmp/openra

%description
A multiplayer reimplementation of the Command & Conquer: Red Alert game 
engine in .NET/Mono, OpenGL, OpenAL and SDL. Has extensive modding support
and includes Command & Conquer as an official mod.

%build

%install
rm -rf $RPM_BUILD_ROOT
cp -r ~/openra-package/built/ $RPM_BUILD_ROOT 

%clean
rm -rf $RPM_BUILD_ROOT

%post
cd /tmp
while true 
do
    read -s -n1 -p "Download and install RA packages? [Y/n]"
    case $REPLY in
        y|Y|"") 
            pushd /tmp/
            wget http://open-ra.org/packages/ra-packages.php
            mkdir -p $RPM_BUILD_ROOT/usr/share/openra/mods/ra/packages
            unzip ra-packages.zip -d $RPM_BUILD_ROOT/usr/share/openra/mods/ra/packages
            rm ra-packages.zip
            popd
            break;;
        n|N) break;;
        *) echo "Please enter y or n.";;
    esac
done

while true 
do
    read -s -n1 -p "Download and install C&C packages? [Y/n]"
    case $REPLY in
        y|Y|"") 
            pushd /tmp/
            wget http://open-ra.org/packages/cnc-packages.php
            mkdir -p $RPM_BUILD_ROOT/usr/share/openra/mods/cnc/packages
            unzip ra-packages.zip -d $RPM_BUILD_ROOT/usr/share/openra/mods/cnc/packages
            rm ra-packages.zip
            popd
            break;;
        n|N) break;;
        *) echo "Please enter y or n.";;
    esac
done

gacutil -i $RPM_BUILD_ROOT/usr/share/openra/thirdparty/Tao/Tao.Cg.dll
gacutil -i $RPM_BUILD_ROOT/usr/share/openra/thirdparty/Tao/Tao.FreeType.dll
gacutil -i $RPM_BUILD_ROOT/usr/share/openra/thirdparty/Tao/Tao.OpenAl.dll
gacutil -i $RPM_BUILD_ROOT/usr/share/openra/thirdparty/Tao/Tao.OpenGl.dll
gacutil -i $RPM_BUILD_ROOT/usr/share/openra/thirdparty/Tao/Tao.Sdl.dll

%files
/usr/bin/openra
/usr/share/openra/*.exe
/usr/share/openra/*.ttf
/usr/share/openra/*.dll
/usr/share/openra/shaders/
/usr/share/openra/mods/
/usr/share/openra/thirdparty/
