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
	echo ""
	$command = Read-Host "Enter command"
}
else
{
	$command = $args
}

if ($command -eq "all")
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
		if (-not (Test-Path $path))
		{
			continue
		}
		$proc = Start-Process $path /t:Rebuild -NoNewWindow -PassThru -Wait
		$proc.WaitForExit()
		if ($proc.ExitCode -ne 0)
		{
			echo "Build failed. If just the development tools failed to build, try installing Visual Studio. You may also still be able to run the game."
		}
		else
		{
			echo "Build succeeded."
		}
		break
	}
	if ($proc -eq $null)
	{
		echo "Unable to locate an appropriate version of MSBuild."
	}
}
elseif ($command -eq "clean")
{
	rm *.pdb
	rm *.config
	rm *.exe
	rm *.dll
	rm mods/*/*.dll
	echo "Clean complete."
}
elseif ($command -eq "version")
{
	$version = git name-rev --name-only --tags --no-undefined HEAD 2>$null
	if ($version -eq $null)
	{
		$version = "git-" + (git rev-parse --short HEAD)
	}
	$mods = @("mods/ra/mod.yaml", "mods/cnc/mod.yaml", "mods/d2k/mod.yaml", "mods/modchooser/mod.yaml")
	foreach ($mod in $mods)
	{
		$replacement = (gc $mod) -Replace "Version:.*", ("Version: {0}" -f $version)
		sc $mod $replacement
	}
	echo ("Version strings set to '{0}'." -f $version)
}
elseif ($command -eq "dependencies")
{
	cp thirdparty/*.dll .
	cp thirdparty/windows/*.dll .
	echo "Dependencies copied."
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
