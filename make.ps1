####### The starting point for the script is the bottom #######

###############################################################
########################## FUNCTIONS ##########################
###############################################################
function All-Command
{
	if ((CheckForDotnet) -eq 1)
	{
		return
	}

	dotnet build -c Release --nologo -p:TargetPlatform=win-x64
	if ($lastexitcode -ne 0)
	{
		Write-Host "Build failed. If just the development tools failed to build, try installing Visual Studio. You may also still be able to run the game." -ForegroundColor Red
	}
	else
	{
		Write-Host "Build succeeded." -ForegroundColor Green
	}

	if (!(Test-Path "IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP") -Or (((get-date) - (get-item "IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP").LastWriteTime) -gt (new-timespan -days 30)))
	{
		echo "Downloading IP2Location GeoIP database."
		$target = Join-Path $pwd.ToString() "IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP"
		(New-Object System.Net.WebClient).DownloadFile("https://download.ip2location.com/lite/IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP", $target)
	}
}

function Clean-Command
{
	if ((CheckForDotnet) -eq 1)
	{
		return
	}

	dotnet clean /nologo
	rm ./bin -r
	rm ./*/bin -r
	rm ./*/obj -r
	Write-Host "Clean complete." -ForegroundColor Green
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
			Write-Host "Not a git repository. The version will remain unchanged." -ForegroundColor Red
		}
	}
	else
	{
		Write-Host "Unable to locate Git. The version will remain unchanged." -ForegroundColor Red
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
		Write-Host ("Version strings set to '{0}'." -f $version)
	}
}

function Test-Command
{
	if ((CheckForUtility) -eq 1)
	{
		return
	}

	Write-Host "Testing mods..." -ForegroundColor Cyan
	Write-Host "Testing Tiberian Sun mod MiniYAML..." -ForegroundColor Cyan
	InvokeCommand "$utilityPath ts --check-yaml"
	Write-Host "Testing Dune 2000 mod MiniYAML..." -ForegroundColor Cyan
	InvokeCommand "$utilityPath d2k --check-yaml"
	Write-Host "Testing Tiberian Dawn mod MiniYAML..." -ForegroundColor Cyan
	InvokeCommand "$utilityPath cnc --check-yaml"
	Write-Host "Testing Red Alert mod MiniYAML..." -ForegroundColor Cyan
	InvokeCommand "$utilityPath ra --check-yaml"
}

function Check-Command
{
	Write-Host "Compiling in debug configuration..." -ForegroundColor Cyan
	dotnet build -c Debug --nologo -p:TargetPlatform=win-x64
	if ($lastexitcode -ne 0)
	{
		Write-Host "Build failed." -ForegroundColor Red
	}

	if ((CheckForUtility) -eq 0)
	{
		Write-Host "Checking for explicit interface violations..." -ForegroundColor Cyan
		InvokeCommand "$utilityPath all --check-explicit-interfaces"

		Write-Host "Checking for incorrect conditional trait interface overrides..." -ForegroundColor Cyan
		InvokeCommand "$utilityPath all --check-conditional-trait-interface-overrides"
	}
}

function Check-Scripts-Command
{
	if ((Get-Command "luac.exe" -ErrorAction SilentlyContinue) -ne $null)
	{
		Write-Host "Testing Lua scripts..." -ForegroundColor Cyan
		foreach ($script in ls "mods/*/maps/*/*.lua")
		{
			luac -p $script
		}
		foreach ($script in ls "lua/*.lua")
		{
			luac -p $script
		}
		foreach ($script in ls "mods/*/bits/scripts/*.lua")
		{
			luac -p $script
		}
		Write-Host "Check completed!" -ForegroundColor Green
	}
	else
	{
		Write-Host "luac.exe could not be found. Please install Lua." -ForegroundColor Red
	}
}

function Docs-Command
{
	if ((CheckForUtility) -eq 1)
	{
		return
	}

	./make.ps1 version
	Invoke-Expression "$utilityPath all --docs" | Out-File -Encoding "UTF8" DOCUMENTATION.md
	Invoke-Expression "$utilityPath all --weapon-docs" | Out-File -Encoding "UTF8" WEAPONS.md
	Invoke-Expression "$utilityPath all --lua-docs" | Out-File -Encoding "UTF8" Lua-API.md
	Invoke-Expression "$utilityPath all --settings-docs" | Out-File -Encoding "UTF8" Settings.md
}

function CheckForUtility
{
	if (Test-Path $utilityPath)
	{
		return 0
	}

	Write-Host "OpenRA.Utility.exe could not be found. Build the project first using the `"all`" command." -ForegroundColor Red
	return 1
}

function CheckForDotnet
{
	if ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)
	{
		Write-Host "The 'dotnet' tool is required to compile OpenRA. Please install the .NET Core SDK or Visual Studio and try again. https://dotnet.microsoft.com/download" -ForegroundColor Red
		return 1
	}

	return 0
}

function WaitForInput
{
	Write-Host "Press enter to continue."
	while ($true)
	{
		if ([System.Console]::KeyAvailable)
		{
			exit
		}
		Start-Sleep -Milliseconds 50
	}
}

function InvokeCommand
{
	param($expression)
	# $? is the return value of the called expression
	# Invoke-Expression itself will always succeed, even if the invoked expression fails
	# So temporarily store the return value in $success
	$expression += '; $success = $?'
	Invoke-Expression $expression
	if ($success -eq $False)
	{
		exit 1
	}
}

###############################################################
############################ Main #############################
###############################################################
if ($PSVersionTable.PSVersion.Major -clt 3)
{
	Write-Host "The makefile requires PowerShell version 3 or higher." -ForegroundColor Red
	Write-Host "Please download and install the latest Windows Management Framework version from Microsoft." -ForegroundColor Red
	WaitForInput
}

if ($args.Length -eq 0)
{
	Write-Host "Command list:"
	Write-Host ""
	Write-Host "  all, a              Builds the game and its development tools."
	Write-Host "  version, v          Sets the version strings for the default mods to the"
	Write-Host "                      latest version for the current Git branch."
	Write-Host "  clean, c            Removes all built and copied files. Use the 'all' and"
	Write-Host "                      'dependencies' commands to restore removed files."
	Write-Host "  test, t             Tests the default mods for errors."
	Write-Host "  check, ck           Checks .cs files for StyleCop violations."
	Write-Host "  check-scripts, cs   Checks .lua files for syntax errors."
	Write-Host "  docs                Generates the trait and Lua API documentation."
	Write-Host ""
	$command = (Read-Host "Enter command").Split(' ', 2)
}
else
{
	$command = $args
}

$env:ENGINE_DIR = ".."
$utilityPath = "bin\OpenRA.Utility.exe"

$execute = $command
if ($command.Length -gt 1)
{
	$execute = $command[0]
}

switch ($execute)
{
	{"all",           "a"  -contains $_} { All-Command }
	{"version",       "v"  -contains $_} { Version-Command }
	{"clean",         "c"  -contains $_} { Clean-Command }
	{"test",          "t"  -contains $_} { Test-Command }
	{"check",         "ck" -contains $_} { Check-Command }
	{"check-scripts", "cs" -contains $_} { Check-Scripts-Command }
	 "docs"                              { Docs-Command }
	Default { Write-Host ("Invalid command '{0}'" -f $command) }
}

#In case the script was called without any parameters we keep the window open
if ($args.Length -eq 0)
{
	WaitForInput
}
