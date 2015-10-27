function FindMSBuild
{
	$msBuildVersions = @("4.0")
	foreach ($msBuildVersion in $msBuildVersions)
	{
		$key = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\{0}" -f $msBuildVersion
		$property = Get-ItemProperty $key -ErrorAction SilentlyContinue
		if ($property -eq $null -or $property.MSBuildToolsPath -eq $null)
		{
			continue
		}
		$path = Join-Path $property.MSBuildToolsPath -ChildPath "MSBuild.exe"
		if (Test-Path $path)
		{
			return $path
		}
	}
	return $null
}

function UtilityNotFound
{
	Write-Output "OpenRA.Utility.exe could not be found. Build the project first using the `"all`" command."
}

if ($args.Length -eq 0)
{
	Write-Output "Command list:"
	Write-Output ""
	Write-Output "  all             Builds the game and its development tools."
	Write-Output "  dependencies    Copies the game's dependencies into the main game folder."
	Write-Output "  version         Sets the version strings for the default mods to the latest"
	Write-Output "                  version for the current Git branch."
	Write-Output "  clean           Removes all built and copied files. Use the 'all' and"
	Write-Output "                  'dependencies' commands to restore removed files."
	Write-Output "  test            Tests the default mods for errors."
	Write-Output "  check           Checks .cs files for StyleCop violations."
	Write-Output "  check-scripts   Checks .lua files for syntax errors."
	Write-Output "  docs            Generates the trait and Lua API documentation."
	Write-Output ""
	$command = (Read-Host "Enter command").Split(' ', 2)
}
else
{
	$command = $args
}

if ($command -eq "all")
{
	$msBuild = FindMSBuild
	$msBuildArguments = "/t:Rebuild /nr:false"
	if ($msBuild -eq $null)
	{
		Write-Output "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		$proc = Start-Process $msBuild $msBuildArguments -NoNewWindow -PassThru -Wait
		if ($proc.ExitCode -ne 0)
		{
			Write-Output "Build failed. If just the development tools failed to build, try installing Visual Studio. You may also still be able to run the game."
		}
		else
		{
			Write-Output "Build succeeded."
		}
	}
}
elseif ($command -eq "clean")
{
	$msBuild = FindMSBuild
	$msBuildArguments = "/t:Clean /nr:false"
	if ($msBuild -eq $null)
	{
		Write-Output "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		$proc = Start-Process $msBuild $msBuildArguments -NoNewWindow -PassThru -Wait
		Remove-Item *.dll
		Remove-Item *.dll.config
		Remove-Item mods/*/*.dll
		if (Test-Path thirdparty/download/)
		{
			Remove-Item thirdparty/download -Recurse -Force
		}
		Write-Output "Clean complete."
	}
}
elseif ($command -eq "version")
{	
	if ($command.Length -gt 1)
	{
		$version = $command[1]
	}
	elseif (Get-Command 'git' -ErrorAction SilentlyContinue)
	{
		$version = git name-rev --name-only --tags --no-undefined HEAD 2>$null
		if ($version -eq $null)
		{
			$version = "git-" + (git rev-parse --short HEAD)
		}
	}
	else
	{	
		Write-Output "Unable to locate Git. The version will remain unchanged."
	}
	
	if ($version -ne $null)
	{
		$mods = @("mods/ra/mod.yaml", "mods/cnc/mod.yaml", "mods/d2k/mod.yaml", "mods/ts/mod.yaml", "mods/modchooser/mod.yaml", "mods/all/mod.yaml")
		foreach ($mod in $mods)
		{
			$replacement = (Get-Content $mod) -Replace "Version:.*", ("Version: {0}" -f $version)
			Set-Content $mod $replacement
			$replacement = (Get-Content $mod) -Replace "modchooser:.*", ("modchooser: {0}" -f $version)
			Set-Content $mod $replacement
		}
		Write-Output ("Version strings set to '{0}'." -f $version)
	}
}
elseif ($command -eq "dependencies")
{
	Set-Location thirdparty
	./fetch-thirdparty-deps.ps1
	Copy-Item download/*.dll ..
	Copy-Item download/GeoLite2-Country.mmdb.gz ..
	Copy-Item download/windows/*.dll ..
	Set-Location ..
	Write-Output "Dependencies copied."
}
elseif ($command -eq "test")
{
	if (Test-Path OpenRA.Utility.exe)
	{
		Write-Output "Testing mods..."
		Write-Output "Testing Tiberian Sun mod MiniYAML..."
		./OpenRA.Utility.exe ts --check-yaml
		Write-Output "Testing Dune 2000 mod MiniYAML..."
		./OpenRA.Utility.exe d2k --check-yaml
		Write-Output "Testing Tiberian Dawn mod MiniYAML..."
		./OpenRA.Utility.exe cnc --check-yaml
		Write-Output "Testing Red Alert mod MiniYAML..."
		./OpenRA.Utility.exe ra --check-yaml
	}
	else
	{
		UtilityNotFound
	}
}
elseif ($command -eq "check")
{
	if (Test-Path OpenRA.Utility.exe)
	{
		Write-Output "Checking for code style violations in OpenRA.Platforms.Default..."
		./OpenRA.Utility.exe cnc --check-code-style OpenRA.Platforms.Default
		Write-Output "Checking for code style violations in OpenRA.Platforms.Null..."
		./OpenRA.Utility.exe ra --check-code-style OpenRA.Platforms.Null
		Write-Output "Checking for code style violations in OpenRA.GameMonitor..."
		./OpenRA.Utility.exe ra --check-code-style OpenRA.GameMonitor
		Write-Output "Checking for code style violations in OpenRA.Game..."
		./OpenRA.Utility.exe ra --check-code-style OpenRA.Game
		Write-Output "Checking for code style violations in OpenRA.Mods.Common..."
		./OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.Common
		Write-Output "Checking for code style violations in OpenRA.Mods.RA..."
		./OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.RA
		Write-Output "Checking for code style violations in OpenRA.Mods.Cnc..."
		./OpenRA.Utility.exe cnc --check-code-style OpenRA.Mods.Cnc
		Write-Output "Checking for code style violations in OpenRA.Mods.D2k..."
		./OpenRA.Utility.exe cnc --check-code-style OpenRA.Mods.D2k
		Write-Output "Checking for code style violations in OpenRA.Mods.TS..."
		./OpenRA.Utility.exe cnc --check-code-style OpenRA.Mods.TS
		Write-Output "Checking for code style violations in OpenRA.Utility..."
		./OpenRA.Utility.exe cnc --check-code-style OpenRA.Utility
		Write-Output "Checking for code style violations in OpenRA.Test..."
		./OpenRA.Utility.exe cnc --check-code-style OpenRA.Test
	}
	else
	{
		UtilityNotFound
	}
}
elseif ($command -eq "check-scripts")
{
	if ((Get-Command "luac.exe" -ErrorAction SilentlyContinue) -ne $null)
	{
		Write-Output "Testing Lua scripts..."
		foreach ($script in ls "mods/*/maps/*/*.lua")
		{
			luac -p $script
		}
		foreach ($script in ls "lua/*.lua")
		{
			luac -p $script
		}
		Write-Output "Check completed!"
	}
	else
	{
		Write-Output "luac.exe could not be found. Please install Lua."
	}
}
elseif ($command -eq "docs")
{
	if (Test-Path OpenRA.Utility.exe)
	{
		./make.ps1 version
		./OpenRA.Utility.exe all --docs | Out-File -Encoding "UTF8" DOCUMENTATION.md
		./OpenRA.Utility.exe all --lua-docs | Out-File -Encoding "UTF8" Lua-API.md
	}
	else
	{
		UtilityNotFound
	}
}
else
{
	Write-Output ("Invalid command '{0}'" -f $command)
}

if ($args.Length -eq 0)
{
	Write-Output "Press enter to continue."
	while ($true)
	{
		if ([System.Console]::KeyAvailable)
		{
			break
		}
		Start-Sleep -Milliseconds 50
	}
}
