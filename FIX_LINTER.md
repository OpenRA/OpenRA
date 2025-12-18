Linux Errors:
Run sudo apt-get install lua5.1
  sudo apt-get install lua5.1
  make check-scripts
  make TREAT_WARNINGS_AS_ERRORS=true test
  shell: /usr/bin/bash -e {0}
  env:
    DOTNET_ROOT: /usr/share/dotnet
Reading package lists...
Building dependency tree...
Reading state information...
The following NEW packages will be installed:
  lua5.1
0 upgraded, 1 newly installed, 0 to remove and 10 not upgraded.
Need to get 94.6 kB of archives.
After this operation, 342 kB of additional disk space will be used.
Get:1 file:/etc/apt/apt-mirrors.txt Mirrorlist [144 B]
Get:2 http://azure.archive.ubuntu.com/ubuntu jammy/universe amd64 lua5.1 amd64 5.1.5-8.1build4 [94.6 kB]
Fetched 94.6 kB in 0s (928 kB/s)
Selecting previously unselected package lua5.1.
(Reading database ... 
(Reading database ... 5%
(Reading database ... 10%
(Reading database ... 15%
(Reading database ... 20%
(Reading database ... 25%
(Reading database ... 30%
(Reading database ... 35%
(Reading database ... 40%
(Reading database ... 45%
(Reading database ... 50%
(Reading database ... 55%
(Reading database ... 60%
(Reading database ... 65%
(Reading database ... 70%
(Reading database ... 75%
(Reading database ... 80%
(Reading database ... 85%
(Reading database ... 90%
(Reading database ... 95%
(Reading database ... 100%
(Reading database ... 257188 files and directories currently installed.)
Preparing to unpack .../lua5.1_5.1.5-8.1build4_amd64.deb ...
Unpacking lua5.1 (5.1.5-8.1build4) ...
Setting up lua5.1 (5.1.5-8.1build4) ...
update-alternatives: using /usr/bin/lua5.1 to provide /usr/bin/lua (lua-interpreter) in auto mode
update-alternatives: using /usr/bin/luac5.1 to provide /usr/bin/luac (lua-compiler) in auto mode
Processing triggers for man-db (2.10.2-1) ...
Not building database; man-db/auto-update is not 'true'.

Running kernel seems to be up-to-date.

No services need to be restarted.

No containers need to be restarted.

No user sessions are running outdated binaries.

No VM guests are running outdated hypervisor (qemu) binaries on this host.

Checking for Lua syntax errors...
Compiling in Release mode...
  Determining projects to restore...
  All projects are up-to-date for restore.
  OpenRA.Game -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Game.dll
  OpenRA.Launcher -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.dll
  OpenRA.Utility -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Utility.dll
  OpenRA.Platforms.Default -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Platforms.Default.dll
  OpenRA.Server -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Server.dll
  OpenRA.Mods.Common -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Mods.Common.dll
  OpenRA.Mods.D2k -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Mods.D2k.dll
  OpenRA.Mods.Cnc -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Mods.Cnc.dll
  OpenRA.Test -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Test.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:11.94
Downloading IP2Location GeoIP database.

Testing Tiberian Sun mod MiniYAML...
Testing mod: Mod Content Manager
Testing Fluent references
Testing mod: Tiberian Sun
Testing default sequences for TEMPERATE
Testing default sequences for SNOW
Testing Fluent references
Error: Missing key `dialog-tutorial-resume.prompt` in mod ftl files required by MainMenuLogic.TutorialResumePrompt
Error: Missing key `dialog-tutorial-resume.resume` in mod ftl files required by MainMenuLogic.TutorialResumeButton
Error: Missing key `dialog-tutorial-resume.start-new` in mod ftl files required by MainMenuLogic.TutorialStartNewButton
Error: Missing key `dialog-tutorial-resume.title` in mod ftl files required by MainMenuLogic.TutorialResumeTitle
Testing map: No where to run
Testing map: Hidden Valley
Testing map: A River Runs Near It
Testing map: Cityscape
Testing map: Cliffs of Insanity
Testing map: Drawbridges
Testing map: Fields of Green
Testing map: They All Float
Testing map: Forest Fires
Testing map: Town of Karasjok
Testing map: Oasis Trouble
Testing map: River Raid
Testing map: Hot Springs
Testing map: Sunstroke
Testing map: Tiberium Garden Redux
Testing map: Tactical
Testing map: Terraces
Testing map: The Pit
Testing map: Tiers of Sorrow
Testing map: Tread Lightly
Testing map: Tournament Rift
Testing map: The Way to Uganda
Errors: 4
make: *** [Makefile:123: test] Error 1
Error: Process completed with exit code 2.

Windows Errors:
Run choco install lua --version 5.1.5.52 --no-progress
  choco install lua --version 5.1.5.52 --no-progress
  $ENV:Path = $ENV:Path + ";C:\Program Files (x86)\Lua\5.1\"
  $ENV:TREAT_WARNINGS_AS_ERRORS = "true"
  .\make.ps1 check-scripts
  .\make.ps1 test
  shell: C:\Program Files\PowerShell\7\pwsh.EXE -command ". '{0}'"
  env:
    DOTNET_ROOT: C:\Program Files\dotnet
Chocolatey v2.5.1
Installing the following packages:
lua
By installing, you accept licenses for the packages.
Downloading package from source 'https://community.chocolatey.org/api/v2/'

vcredist2005 v8.0.50727.619501 [Approved]
vcredist2005 package files install completed. Performing other installation steps.
Downloading vcredist2005 64 bit
  from 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x64.EXE'

Download of vcredist_x64.EXE (3.03 MB) completed.
Hashes match.
Installing vcredist2005...
vcredist2005 has been installed.
Downloading vcredist2005 32 bit
  from 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x86.EXE'

Download of vcredist_x86.EXE (2.58 MB) completed.
Hashes match.
Installing vcredist2005...
vcredist2005 has been installed.
  vcredist2005 may be able to be automatically uninstalled.
 The install of vcredist2005 was successful.
  Software installed as 'exe', install location is likely default.
Downloading package from source 'https://community.chocolatey.org/api/v2/'

lua v5.1.5.52 [Approved]
lua package files install completed. Performing other installation steps.
Downloading lua 
  from 'https://github.com/rjpcomputing/luaforwindows/releases/download/v5.1.5-52/LuaForWindows_v5.1.5-52.exe'

Download of LuaForWindows_v5.1.5-52.exe (27.8 MB) completed.
Hashes match.
Installing lua...
lua has been installed.
  lua can be automatically uninstalled.
Environment Vars (like PATH) have changed. Close/reopen your shell to
 see the changes (or in powershell/cmd.exe just type `refreshenv`).
 The install of lua was successful.
  Deployed to 'C:\Program Files (x86)\Lua\5.1\'

Chocolatey installed 2/2 packages. 
 See the log for details (C:\ProgramData\chocolatey\logs\chocolatey.log).
Testing Lua scripts...
Check completed!
Testing mods...

Testing Tiberian Sun mod MiniYAML...
Testing mod: Mod Content Manager
Testing Fluent references
Testing mod: Tiberian Sun
Testing default sequences for TEMPERATE
Testing default sequences for SNOW
Testing Fluent references
Error: Missing key `dialog-tutorial-resume.prompt` in mod ftl files required by MainMenuLogic.TutorialResumePrompt
Error: Missing key `dialog-tutorial-resume.resume` in mod ftl files required by MainMenuLogic.TutorialResumeButton
Error: Missing key `dialog-tutorial-resume.start-new` in mod ftl files required by MainMenuLogic.TutorialStartNewButton
Error: Missing key `dialog-tutorial-resume.title` in mod ftl files required by MainMenuLogic.TutorialResumeTitle
Testing map: No where to run
Testing map: Hidden Valley
Testing map: A River Runs Near It
Testing map: Cityscape
Testing map: Cliffs of Insanity
Testing map: Drawbridges
Testing map: Fields of Green
Testing map: They All Float
Testing map: Forest Fires
Testing map: Town of Karasjok
Testing map: Oasis Trouble
Testing map: River Raid
Testing map: Hot Springs
Testing map: Sunstroke
Testing map: Tiberium Garden Redux
Testing map: Tactical
Testing map: Terraces
Testing map: The Pit
Testing map: Tiers of Sorrow
Testing map: Tread Lightly
Testing map: Tournament Rift
Testing map: The Way to Uganda
Errors: 4
Error: Process completed with exit code 1.