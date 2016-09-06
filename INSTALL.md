To run OpenRA, several files are needed from the original game disks.
A minimal asset pack can also be downloaded and installed by the game.

The following lists per-platform dependencies required to build from source.

Windows
=======

* [Windows PowerShell >= 4.0](http://microsoft.com/powershell)
* [.NET Framework >= 4.5 (Client Profile)](http://www.microsoft.com/en-us/download/details.aspx?id=30653)
* [SDL 2](http://www.libsdl.org/download-2.0.php) (included)
* [FreeType](http://gnuwin32.sourceforge.net/packages/freetype.htm) (included)
* [zlib](http://gnuwin32.sourceforge.net/packages/zlib.htm) (included)
* [OpenAL](http://kcat.strangesoft.net/openal.html) (included)
* [liblua 5.1](http://luabinaries.sourceforge.net/download.html) (included)

You need to fetch the thirdparty dependencies using [NuGet](http://www.nuget.org) and place them at the appropriate places by typing `make dependencies` in a command terminal.

To compile OpenRA, open the `OpenRA.sln` solution in the main folder, build it from the command-line with MSBuild or use the Makefile analogue command `make all` scripted in PowerShell syntax.

Run the game with `OpenRA.Game.exe Game.Mod=ra` for Red Alert or `OpenRA.Game.exe Game.Mod=cnc` for Tiberian Dawn.

Linux
=====

Use `make dependencies` to map the native libraries to your system, fetch the remaining CLI dependencies using [NuGet](http://www.nuget.org) and place them at the appropriate places.

To compile OpenRA, run `make all` from the command line.

Run with either `launch-game.sh` or `mono --debug OpenRA.Game.exe`.

Type `sudo make install-all` for system wide installation. Run `make install-linux-shortcuts` to get startup scripts, icons and desktop files. You can then run from the `openra` shortcut.

Debian/Ubuntu
-------------

* nuget
* mono-devel
* libfreetype6
* libopenal1
* liblua5.1-0
* libsdl2-2.0-0
* xdg-utils
* zenity
* curl

openSUSE
--------

* mono-devel
* nuget
* openal
* freetype2
* SDL2
* lua51
* xdg-utils
* zenity
* curl

Gentoo
------

* dev-lang/mono
* dev-dotnet/libgdiplus
* dev-dotnet/nuget
* media-libs/freetype:2
* media-libs/libsdl2
* media-libs/openal
* virtual/jpeg
* virtual/opengl
* dev-lang/lua-5.1.5
* x11-misc/xdg-utils
* gnome-extra/zenity
* net-misc/curl

OSX
=====

Use `make dependencies` to map the native libraries to your system.

To compile OpenRA, run `make` from the command line.

Run with `mono --debug OpenRA.Game.exe`.
