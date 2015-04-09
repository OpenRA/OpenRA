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

if ($args.Length -eq 0)
{
	echo "Command list:"
	echo ""
	echo "  all             Builds the game and its development tools."
	echo "  dependencies    Copies the game's dependencies into the main game folder."
	echo "  version         Sets the version strings for the default mods to the latest"
	echo "                  version for the current Git branch."
	echo "  clean           Removes all built and copied files. Use the 'all' and"
	echo "                  'dependencies' commands to restore removed files."
	echo "  test            Tests the default mods for errors."
	echo "  check           Checks .cs files for StyleCop violations."
	echo "  docs            Generates the trait and Lua API documentation."
	echo ""
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
elseif ($command -eq "clean")
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
		rm *.dll # delete third party dependencies
		rm *.config
		rm mods/*/*.dll
		echo "Clean complete."
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
		echo "Unable to locate Git. The version will remain unchanged."
	}
	
	if ($version -ne $null)
	{
		$mods = @("mods/ra/mod.yaml", "mods/cnc/mod.yaml", "mods/d2k/mod.yaml", "mods/modchooser/mod.yaml")
		foreach ($mod in $mods)
		{
			$replacement = (gc $mod) -Replace "Version:.*", ("Version: {0}" -f $version)
			sc $mod $replacement
		}
		echo ("Version strings set to '{0}'." -f $version)
	}
}
elseif ($command -eq "dependencies")
{
	cd thirdparty
	./fetch-thirdparty-deps.ps1
	cp *.dll ..
	cp windows/*.dll ..
	cd ..
	echo "Dependencies copied."
}
elseif ($command -eq "test")
{
	echo "Testing mods..."
	echo "Testing Red Alert mod MiniYAML..."
	./OpenRA.Utility.exe ra --check-yaml
	echo "Testing Tiberian Dawn mod MiniYAML..."
	./OpenRA.Utility.exe cnc --check-yaml
	echo "Testing Dune 2000 mod MiniYAML..."
	./OpenRA.Utility.exe d2k --check-yaml
	echo "Testing Tiberian Sun mod MiniYAML..."
	./OpenRA.Utility.exe ts --check-yaml
}
elseif ($command -eq "check")
{
	echo "Checking for code style violations in OpenRA.Renderer.Null..."
	./OpenRA.Utility.exe ra --check-code-style OpenRA.Renderer.Null
	echo "Checking for code style violations in OpenRA.GameMonitor..."
	./OpenRA.Utility.exe ra --check-code-style OpenRA.GameMonitor
	echo "Checking for code style violations in OpenRA.Game..."
	./OpenRA.Utility.exe ra --check-code-style OpenRA.Game
	echo "Checking for code style violations in OpenRA.Mods.Common..."
	./OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.Common
	echo "Checking for code style violations in OpenRA.Mods.RA..."
	./OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.RA
	echo "Checking for code style violations in OpenRA.Mods.Cnc..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Mods.Cnc
	echo "Checking for code style violations in OpenRA.Mods.D2k..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Mods.D2k
	echo "Checking for code style violations in OpenRA.Mods.TS..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Mods.TS
	echo "Checking for code style violations in OpenRA.Editor..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Editor
	echo "Checking for code style violations in OpenRA.Renderer.Sdl2..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Renderer.Sdl2
	echo "Checking for code style violations in OpenRA.Utility..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Utility
	echo "Checking for code style violations in OpenRA.Test..."
	./OpenRA.Utility.exe cnc --check-code-style OpenRA.Test
}
elseif ($command -eq "docs")
{
	./make.ps1 version
	./OpenRA.Utility.exe d2k --docs | Out-File -Encoding "UTF8" DOCUMENTATION.md
	./OpenRA.Utility.exe ra --lua-docs | Out-File -Encoding "UTF8" Lua-API.md
}
else
{
	echo ("Invalid command '{0}'" -f $command)
}

if ($args.Length -eq 0)
{
	echo "Press enter to continue."
	while ($true)
	{
		if ([System.Console]::KeyAvailable)
		{
			break
		}
		Start-Sleep -Milliseconds 50
	}
}
