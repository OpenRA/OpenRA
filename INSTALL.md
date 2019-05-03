To run OpenRA, several files are needed from the original game disks.
A minimal asset pack can also be downloaded and installed by the game.

The following lists per-platform dependencies required to build from source.

Windows
=======

Compiling OpenRA requires the following dependencies:
* [Windows PowerShell >= 4.0](http://microsoft.com/powershell) (included by default in recent Windows 10 versions)
* [.NET Framework 4.6.1 (Developer Pack)](https://dotnet.microsoft.com/download/dotnet-framework/net461) (or via Visual Studio 2017)
* [.NET Core 2.2 SDK](https://dotnet.microsoft.com/download/dotnet-core/2.2) (or via Visual Studio 2017)

Type `make dependencies` in a command terminal to download pre-compiled native libraries for:
* [SDL 2](http://www.libsdl.org/download-2.0.php)
* [FreeType](http://gnuwin32.sourceforge.net/packages/freetype.htm)
* [zlib](http://gnuwin32.sourceforge.net/packages/zlib.htm)
* [OpenAL](http://kcat.strangesoft.net/openal.html)
* [liblua 5.1](http://luabinaries.sourceforge.net/download.html)

To compile OpenRA, open the `OpenRA.sln` solution in the main folder, build it from the command-line with MSBuild or use the Makefile analogue command `make all` scripted in PowerShell syntax.

Run the game with `launch-game.cmd`. It can be handed arguments that specify the exact mod one wishes to run, for example, run `launch-game.cmd Game.Mod=ra` to launch Red Alert, `launch-game.cmd Game.Mod=cnc` to start Tiberian dawn or `launch-game.cmd Game.Mod=d2k` to launch Dune 2000.

Linux
=====

Mono, version 5.4 or later, is required to compile OpenRA. You can add the [upstream mono repository](https://www.mono-project.com/download/stable/#download-lin) for your distro to obtain the latest version if your system packages are not sufficient.

Use `make dependencies` to map the native libraries to your system and fetch the remaining CLI dependencies to place them at the appropriate places.

To compile OpenRA, run `make all` from the command line. After this one can run the game with `./launch-game.sh`. It is also possible to specify the mod you wish to run from the command line, e.g. with `./launch-game.sh Game.Mod=ts` if you wish to try the experimental Tiberian Sun mod. 

Type `sudo make install` for system-wide installation. Run `sudo make install-linux-shortcuts` to get startup scripts, icons and desktop files. You can then run the Red Alert by executing the `openra-ra` command, the Dune 2000 mod by running the `openra-d2k` command and Tiberian Dawn by the `openra-cnc` command. Alternatively, you can also run these mods by clicking on their desktop shortcuts if you ran `sudo make install-linux-shortcuts`. 

Arch Linux
----------

It is important to note there is an unofficial [`openra-git`](https://aur.archlinux.org/packages/openra-git) package in the Arch User Repository (AUR) of Arch Linux. If manually compiling is the way you wish to go the build and runtime dependencies can be installed with:

```
sudo pacman -S mono openal libgl freetype2 sdl2 lua51 xdg-utils zenity
```

Debian/Ubuntu
-------------

:warning: The `mono` packages in the Ubuntu < 19.04 and Debian < 10 repositories are too old to support OpenRA. :warning:

See the instructions under the *Linux* section above to upgrade `mono` using the upstream releases if needed.

```
sudo apt install mono-devel libfreetype6 libopenal1 liblua5.1-0 libsdl2-2.0-0 xdg-utils zenity wget
```

Fedora
------

:warning: The `mono` packages in the Fedora repositories are too old to support OpenRA. :warning:

See the instructions under the *Linux* section above to upgrade `mono` using the upstream releases.


```
sudo dnf install "pkgconfig(mono)" SDL2 freetype "lua = 5.1" openal-soft xdg-utils zenity
```

Gentoo
------

```
sudo emerge -av dev-lang/mono dev-dotnet/libgdiplus media-libs/freetype:2 media-libs/libsdl2 media-libs/openal virtual/jpeg virtual/opengl '=dev-lang/lua-5.1.5*' x11-misc/xdg-utils gnome-extra/zenity
```

Mageia
------

```
sudo dnf install "pkgconfig(mono)" SDL2 freetype "lib*lua5.1" "lib*freetype2" "lib*sdl2.0_0" openal-soft xdg-utils zenity
```

openSUSE
--------

```
sudo zypper in mono-devel openal-soft freetype2 SDL2 lua51 xdg-utils zenity
```

Red Hat Enterprise Linux (and rebuilds, e.g. CentOS)
----------------------------------------------------

The EPEL repository is required in order for the following command to run properly. 

```
sudo yum install "pkgconfig(mono)" SDL2 freetype "lua = 5.1" openal-soft xdg-utils zenity
```

OSX
=====

Before compiling OpenRA you must install the following dependencies:
* [Mono >= 5.4](https://www.mono-project.com/download/stable/#download-mac)

Use `make dependencies` to download pre-compiled native libraries for:
* [SDL 2](http://www.libsdl.org/download-2.0.php)
* [FreeType](http://gnuwin32.sourceforge.net/packages/freetype.htm)
* [OpenAL](http://kcat.strangesoft.net/openal.html)
* [liblua 5.1](http://luabinaries.sourceforge.net/download.html)

To compile OpenRA, run `make` from the command line.

Run with `./launch-game.sh`.
