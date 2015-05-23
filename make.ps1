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
	if ($msBuild -eq $null)
	{
		echo "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		$proc = Start-Process $msBuild /t:Rebuild -NoNewWindow -PassThru -Wait
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
	if ($msBuild -eq $null)
	{
		echo "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		$proc = Start-Process $msBuild /t:Clean -NoNewWindow -PassThru -Wait
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
	cp thirdparty/*.dll .
	cp thirdparty/windows/*.dll .
	echo "Dependencies copied."
}
elseif ($command -eq "test")
{
	echo "Testing mods..."
	echo "OpenRA.Lint: checking Red Alert mod MiniYAML..."
	./OpenRA.Lint.exe --verbose ra
	echo "OpenRA.Lint: checking Tiberian Dawn mod MiniYAML..."
	./OpenRA.Lint.exe --verbose cnc
	echo "OpenRA.Lint: checking Dune 2000 mod MiniYAML..."
	./OpenRA.Lint.exe --verbose d2k
	echo "OpenRA.Lint: checking Tiberian Sun mod MiniYAML..."
	./OpenRA.Lint.exe --verbose ts
}
else
{
	echo ("Invalid command '{0}'" -f $command)
}

if ($args.Length -eq 0)
{
	echo "Press enter to continue."
	[System.Console]::ReadKey($true) >$null
}
