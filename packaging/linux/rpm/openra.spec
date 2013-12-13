%define name openra
%define version {VERSION_FIELD}
%define root {ROOT_DIR}
%define _use_internal_dependency_generator 0
%define __find_provides ""
%define __find_requires ""
Name: %{name}
Version: %{version}
Release: 1
Summary: Open Source rebuild of the Red Alert game engine using Mono/OpenGL.
License: GPL-3.0
URL: http://open-ra.org
Group: Amusements/Games
Packager: Matthew Bowra-Dean <matthew@ijw.co.nz>
Requires: mono-core mono-devel SDL openal
%if 0%{?fedora}
Requires: freetype
%else
Requires: freetype2
%endif
Prefix: /usr
Source: %{name}-%{version}.tar.gz
BuildRoot: /tmp/openra

%description
A multiplayer reimplementation of the Command & Conquer: Red Alert game
engine in .NET/Mono, OpenGL, OpenAL and SDL. Has extensive modding support
and includes Command & Conquer: Tiberian Dawn as an official mod.

%build

%install
rm -rf $RPM_BUILD_ROOT
cp -r %{root} $RPM_BUILD_ROOT

%clean
rm -rf $RPM_BUILD_ROOT

%files
/usr/bin/openra
/usr/bin/openra-editor
/usr/share/applications/*.desktop
/usr/share/icons/hicolor/*/apps/*.png
/usr/share/icons/hicolor/*/apps/*.svg
