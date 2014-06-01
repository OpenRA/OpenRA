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
Url: http://openra.net
Group: Amusements/Games
Packager: Matthias Mailänder <matthias@mailaender.name>
Requires: mono-core
Requires: mono-winforms
Requires: openal
Requires: libfreetype.so.6
Requires: libasound.so.2
Requires: libc.so.6
Requires: libdl.so.2
Requires: libm.so.6
Requires: libpthread.so.0
Requires: librt.so.1
Requires: xdg-utils
Requires: zenity

Prefix: /usr
Source: %{name}-%{version}.tar.gz
BuildRoot: /tmp/openra

%description
A reimplementation of the Command & Conquer: Red Alert game engine
using .NET/Mono, OpenGL, OpenAL and SDL. It includes reimagninations
of Command & Conquer: Red Alert, Command & Conquer: Tiberian Dawn
as well as Dune 2000.

%build

%install
rm -rf %{buildroot}
cp -r %{root} %{buildroot}

%clean
rm -rf %{buildroot}

%files
/usr/bin/openra
/usr/bin/openra-editor
/usr/share/applications/*.desktop
/usr/share/doc/openra/*
/usr/share/icons/hicolor/*/apps/*.png
/usr/share/icons/hicolor/*/apps/*.svg
