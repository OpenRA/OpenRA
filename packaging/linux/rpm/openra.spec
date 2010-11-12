%define name openra
%define version {VERSION_FIELD}
%define root {ROOT_DIR}
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
cp -r %{root} $RPM_BUILD_ROOT 

%clean
rm -rf $RPM_BUILD_ROOT

%post
cd $RPM_BUILD_ROOT/usr/share/openra
while true 
do
    read -s -n1 -p "Download and install RA packages? [Y/n]"
    case $REPLY in
        y|Y|"") 
            mono OpenRA.Utility.exe --download-packages=ra
            break;;
        n|N)
            echo "The RA packages will need to be manually extracted from http://open-ra.org/get-dependency.php?file=ra-packages \
            to /usr/share/openra/mods/ra/packages before the RA mod will work." 
            break;;
        *) echo "Please enter y or n.";;
    esac
done

while true 
do
    read -s -n1 -p "Download and install C&C packages? [Y/n]"
    case $REPLY in
        y|Y|"") 
            mono OpenRA.Utility.exe --download-packages=cnc
            break;;
        n|N)
            echo "The C&C packages will need to be manually extracted from http://open-ra.org/get-dependency.php?file=cnc-packages \
            to /usr/share/openra/mods/cnc/packages before the C&C mod will work." 
            break;;
        *) echo "Please enter y or n.";;
    esac
done

%files
