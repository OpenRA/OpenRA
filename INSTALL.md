To run OpenRA, several files are needed from the original game disks.
A minimal asset pack can also be downloaded and installed by the game.

The following lists per-platform dependencies required to build from source.

Windows
=======

* [.NET Framework >= 4.0 (Client Profile)](http://www.microsoft.com/en-us/download/details.aspx?id=17113)
* [SDL 2 (included)](http://www.libsdl.org/download-2.0.php)
* [FreeType (included)](http://gnuwin32.sourceforge.net/packages/freetype.htm)
* [zlib (included)](http://gnuwin32.sourceforge.net/packages/zlib.htm)
* [OpenAL (included)](http://kcat.strangesoft.net/openal.html)

To compile OpenRA, open the `OpenRA.sln` solution in the main folder,
or build it from the command-line with MSBuild.

Copy both the native DLLs from `.\thirdparty\windows`
and the CLI images from `.\thirdparty` to the main folder.

Run the game with `OpenRA.Game.exe Game.Mod=ra` for Red Alert
or `OpenRA.Game.exe Game.Mod=cnc` for Tiberian Dawn.

Linux
=====

Run `./configure` to map the native libraries to your system.
To compile OpenRA, run `make all` from the command line.
Run with either `launch-game.sh' or `mono --debug OpenRA.Game.exe'.

Type 'sudo make install-all' for system wide installation. You
can then run from the `openra` shortcut.

Debian/Ubuntu
-------------

* mono-dmcs
* libmono-winforms4.0-cil
* cli-common-dev (>= 2.10)
* freetype
* openal

openSUSE
--------

* mono-devel
* openal
* freetype2
* SDL2

Gentoo
------

* dev-lang/mono
* dev-dotnet/libgdiplus
* media-libs/openal
