To run OpenRA, several files are needed from the original game disks.
A minimal asset pack can also be downloaded and installed by the game.

The following lists per-platform dependencies required to build from source.

Windows
=======

Compiling OpenRA requires the following dependencies:
* [Windows PowerShell >= 4.0](http://microsoft.com/powershell) (included by default in recent Windows 10 versions)
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (or via Visual Studio)

To compile OpenRA, open the `OpenRA.sln` solution in the main folder, build it from the command-line with `dotnet` or use the Makefile analogue command `make all` scripted in PowerShell syntax.

Run the game with `launch-game.cmd`. It can be handed arguments that specify the exact mod one wishes to run, for example, run `launch-game.cmd Game.Mod=ra` to launch Red Alert, `launch-game.cmd Game.Mod=cnc` to start Tiberian dawn or `launch-game.cmd Game.Mod=d2k` to launch Dune 2000.

Linux
=====

.NET 6 or Mono (version 6.12 or later) is required to compile OpenRA. We recommend using .NET 6 when possible, as Mono is poorly packaged by most Linux distributions (e.g. missing the required `msbuild` toolchain), and has been deprecated as a standalone project.

The [.NET 6 download page](https://dotnet.microsoft.com/download/dotnet/6.0) provides repositories for various package managers and binary releases for several architectures. If you prefer to use Mono, we suggest adding the [upstream repository](https://www.mono-project.com/download/stable/#download-lin) for your distro to obtain the latest version and the `msbuild` toolchain.

To compile OpenRA, run `make` from the command line (or `make RUNTIME=mono` if using Mono). After this one can run the game with `./launch-game.sh`. It is also possible to specify the mod you wish to run from the command line, e.g. with `./launch-game.sh Game.Mod=ts` if you wish to try the experimental Tiberian Sun mod.

The default behaviour on the x86_64 architecture is to download several pre-compiled native libraries using the Nuget packaging manager. If you prefer to use system libraries, compile instead using `make TARGETPLATFORM=unix-generic`.

If you choose to use system libraries, or your system is not x86_64, you will need to install [SDL 2](https://www.libsdl.org/download-2.0.php), [FreeType](http://gnuwin32.sourceforge.net/packages/freetype.htm), [OpenAL](https://openal-soft.org/), and [liblua 5.1](http://luabinaries.sourceforge.net/download.html) before compiling OpenRA.

These can be installed using your package manager on various distros:

<details><summary>Arch Linux</summary>

```
sudo pacman -S openal libgl freetype2 sdl2 lua51
```
</details>
<details><summary>Debian/Ubuntu</summary>

```
sudo apt install libfreetype6 libopenal1 liblua5.1-0 libsdl2-2.0-0
```
</details>
<details><summary>Fedora</summary>

```
sudo dnf install SDL2 freetype "lua = 5.1" openal-soft
```
</details>
<details><summary>Gentoo</summary>

```
sudo emerge -av media-libs/freetype:2 media-libs/libsdl2 media-libs/openal virtual/opengl '=dev-lang/lua-5.1.5*'
```
</details>
<details><summary>Mageia</summary>

```
sudo dnf install SDL2 freetype "lib*lua5.1" "lib*freetype2" "lib*sdl2.0_0" openal-soft
```
</details>
<details><summary>openSUSE</summary>

```
sudo zypper in openal-soft freetype2 SDL2 lua51
```
</details>
<details><summary>Red Hat Enterprise Linux (and rebuilds, e.g. CentOS)</summary>
The EPEL repository is required in order for the following command to run properly.

```
sudo yum install SDL2 freetype "lua = 5.1" openal-soft
```
</details>

Type `sudo make install` for system-wide installation. Run `sudo make install-linux-shortcuts` to get startup scripts, icons and desktop files. You can then run the Red Alert by executing the `openra-ra` command, the Dune 2000 mod by running the `openra-d2k` command and Tiberian Dawn by the `openra-cnc` command. Alternatively, you can also run these mods by clicking on their desktop shortcuts if you ran `sudo make install-linux-shortcuts`.

macOS
=====

[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) or [Mono](https://www.mono-project.com/download/stable/#download-mac) (version 6.12 or later) is required to compile OpenRA. We recommend using .NET 6 unless you are running a very old version of macOS (10.9 through 10.14).

To compile OpenRA, run `make` from the command line (or `make RUNTIME=mono` if using Mono). Run with `./launch-game.sh`.
