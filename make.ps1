####### The starting point for the script is the bottom #######

###############################################################
########################## FUNCTIONS ##########################
###############################################################
function All-Command 
{
	Dependencies-Command 
	$msBuild = FindMSBuild
	$msBuildArguments = "/t:Rebuild /nr:false"
	if ($msBuild -eq $null)
	{
		echo "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		$proc = Start-Process $msBuild $msBuildArguments -NoNewWindow -PassThru -Wait
		if ($proc.ExitCode -ne 0)
		{
			echo "Build failed. If just the development tools failed to build, try installing Visual Studio. You may also still be able to run the game."
		}
		else
		{
			echo "Build succeeded."
		}
	}
}

function Clean-Command 
{
	$msBuild = FindMSBuild
	$msBuildArguments = "/t:Clean /nr:false"
	if ($msBuild -eq $null)
	{
		echo "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		$proc = Start-Process $msBuild $msBuildArguments -NoNewWindow -PassThru -Wait
		rm *.dll
		rm mods/*/*.dll
		Get-ChildItem *.dll.config -exclude OpenRA.Platforms.Default.dll.config | Remove-Item
		rm *.pdb
		rm mods/*/*.pdb
		rm *.exe
		rm ./*/bin -r
		rm ./*/obj -r
		if (Test-Path thirdparty/download/)
		{
			rmdir thirdparty/download -Recurse -Force
		}
		echo "Clean complete."
	}
}

function Version-Command 
{
	if ($command.Length -gt 1)
	{
		$version = $command[1]
	}
	elseif (Get-Command 'git' -ErrorAction SilentlyContinue)
	{
		$gitRepo = git rev-parse --is-inside-work-tree
		if ($gitRepo)
		{
			$version = git name-rev --name-only --tags --no-undefined HEAD 2>$null
			if ($version -eq $null)
			{
				$version = "git-" + (git rev-parse --short HEAD)
			}
		}
		else
		{
			echo "Not a git repository. The version will remain unchanged."
		}
	}
	else
	{	
		echo "Unable to locate Git. The version will remain unchanged."
	}
	
	if ($version -ne $null)
	{
		$version | out-file ".\VERSION"
		$mods = @("mods/ra/mod.yaml", "mods/cnc/mod.yaml", "mods/d2k/mod.yaml", "mods/ts/mod.yaml", "mods/modcontent/mod.yaml", "mods/all/mod.yaml")
		foreach ($mod in $mods)
		{
			$replacement = (gc $mod) -Replace "Version:.*", ("Version: {0}" -f $version)
			sc $mod $replacement

			$prefix = $(gc $mod) | Where { $_.ToString().EndsWith(": User") }
			if ($prefix -and $prefix.LastIndexOf("/") -ne -1)
			{
				$prefix = $prefix.Substring(0, $prefix.LastIndexOf("/"))
			}
			$replacement = (gc $mod) -Replace ".*: User", ("{0}/{1}: User" -f $prefix, $version)
			sc $mod $replacement
		}
		echo ("Version strings set to '{0}'." -f $version)
	}
}

function Dependencies-Command
{
	cd thirdparty
	./fetch-thirdparty-deps.ps1
	cp download/*.dll ..
	cp download/GeoLite2-Country.mmdb.gz ..
	cp download/windows/*.dll ..
	cd ..
	echo "Dependencies copied."
}

function Test-Command
{
	if (Test-Path OpenRA.Utility.exe)
	{
		echo "Testing mods..."
		echo "Testing Tiberian Sun mod MiniYAML..."
		./OpenRA.Utility.exe ts --check-yaml
		echo "Testing Dune 2000 mod MiniYAML..."
		./OpenRA.Utility.exe d2k --check-yaml
		echo "Testing Tiberian Dawn mod MiniYAML..."
		./OpenRA.Utility.exe cnc --check-yaml
		echo "Testing Red Alert mod MiniYAML..."
		./OpenRA.Utility.exe ra --check-yaml
	}
	else
	{
		UtilityNotFound
	}
}

function Check-Command {
	if (Test-Path OpenRA.Utility.exe)
	{
		echo "Checking for explicit interface violations..."
		./OpenRA.Utility.exe all --check-explicit-interfaces
	}
	else
	{
		UtilityNotFound
	}

	if (Test-Path OpenRA.StyleCheck.exe)
	{
		echo "Checking for code style violations in OpenRA.Platforms.Default..."
		./OpenRA.StyleCheck.exe OpenRA.Platforms.Default
		echo "Checking for code style violations in OpenRA.Game..."
		./OpenRA.StyleCheck.exe OpenRA.Game
		echo "Checking for code style violations in OpenRA.Mods.Common..."
		./OpenRA.StyleCheck.exe OpenRA.Mods.Common
		echo "Checking for code style violations in OpenRA.Mods.Cnc..."
		./OpenRA.StyleCheck.exe OpenRA.Mods.Cnc
		echo "Checking for code style violations in OpenRA.Mods.D2k..."
		./OpenRA.StyleCheck.exe OpenRA.Mods.D2k
		echo "Checking for code style violations in OpenRA.Utility..."
		./OpenRA.StyleCheck.exe OpenRA.Utility
		echo "Checking for code style violations in OpenRA.Test..."
		./OpenRA.StyleCheck.exe OpenRA.Test
	}
	else
	{
		echo "OpenRA.StyleCheck.exe could not be found. Build the project first using the `"all`" command."
	}
}

function Check-Scripts-Command
{
	if ((Get-Command "luac.exe" -ErrorAction SilentlyContinue) -ne $null)
	{
		echo "Testing Lua scripts..."
		foreach ($script in ls "mods/*/maps/*/*.lua")
		{
			luac -p $script
		}
		foreach ($script in ls "lua/*.lua")
		{
			luac -p $script
		}
		echo "Check completed!"
	}
	else
	{
		echo "luac.exe could not be found. Please install Lua."
	}
}

function Docs-Command
{
	if (Test-Path OpenRA.Utility.exe)
	{
		./make.ps1 version
		./OpenRA.Utility.exe all --docs | Out-File -Encoding "UTF8" DOCUMENTATION.md
		./OpenRA.Utility.exe all --weapon-docs | Out-File -Encoding "UTF8" WEAPONS.md
		./OpenRA.Utility.exe all --lua-docs | Out-File -Encoding "UTF8" Lua-API.md
		./OpenRA.Utility.exe all --settings-docs | Out-File -Encoding "UTF8" Settings.md
	}
	else
	{
		UtilityNotFound
	}
}

function FindMSBuild
{
	$key = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0"
	$property = Get-ItemProperty $key -ErrorAction SilentlyContinue
	if ($property -eq $null -or $property.MSBuildToolsPath -eq $null)
	{
		return $null
	}

	$path = Join-Path $property.MSBuildToolsPath -ChildPath "MSBuild.exe"
	if (Test-Path $path)
	{
		return $path
	}

	return $null
}

function UtilityNotFound
{
	echo "OpenRA.Utility.exe could not be found. Build the project first using the `"all`" command."
}

function WaitForInput
{
	echo "Press enter to continue."
	while ($true)
	{
		if ([System.Console]::KeyAvailable)
		{
			exit
		}
		Start-Sleep -Milliseconds 50
	}
}

###############################################################
############################ Main #############################
###############################################################
if ($PSVersionTable.PSVersion.Major -clt 3)
{
	echo "The makefile requires PowerShell version 3 or higher."
	echo "Please download and install the latest Windows Management Framework version from Microsoft."
	WaitForInput
}

if ($args.Length -eq 0)
{
	echo "Command list:"
	echo ""
	echo "  all, a              Builds the game and its development tools."
	echo "  dependencies, d     Copies the game's dependencies into the main game folder."
	echo "  version, v          Sets the version strings for the default mods to the"
	echo "                      latest version for the current Git branch."
	echo "  clean, c            Removes all built and copied files. Use the 'all' and"
	echo "                      'dependencies' commands to restore removed files."
	echo "  test, t             Tests the default mods for errors."
	echo "  check, ck           Checks .cs files for StyleCop violations."
	echo "  check-scripts, cs   Checks .lua files for syntax errors."
	echo "  docs                Generates the trait and Lua API documentation."
	echo ""
	$command = (Read-Host "Enter command").Split(' ', 2)
}
else
{
	$command = $args
}

$execute = $command
if ($command.Length -gt 1)
{
	$execute = $command[0]
}

switch ($execute)
{
	{"all",           "a"  -contains $_} { All-Command }
	{"dependencies",  "d"  -contains $_} { Dependencies-Command }
	{"version",       "v"  -contains $_} { Version-Command }
	{"clean",         "c"  -contains $_} { Clean-Command }
	{"test",          "t"  -contains $_} { Test-Command }
	{"check",         "ck" -contains $_} { Check-Command }
	{"check-scripts", "cs" -contains $_} { Check-Scripts-Command }
	 "docs"                              { Docs-Command }
	Default { echo ("Invalid command '{0}'" -f $command) }
}

#In case the script was called without any parameters we keep the window open 
if ($args.Length -eq 0)
{
	WaitForInput
}
