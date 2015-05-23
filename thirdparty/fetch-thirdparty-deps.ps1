mkdir windows -Force >$null

if (!(Test-Path "nuget.exe"))
{
	echo "Fetching NuGet."
	# Work around PowerShell's Invoke-WebRequest not being available on some versions of PowerShell by using the BCL.
	# To do that we need to work around further and use absolute paths because DownloadFile is not aware of PowerShell's current directory.
	$target = Join-Path $pwd.ToString() "nuget.exe"
	(New-Object System.Net.WebClient).DownloadFile("http://nuget.org/nuget.exe", $target)
}

if (!(Test-Path "StyleCopPlus.dll"))
{
	echo "Fetching StyleCopPlus from NuGet."
	./nuget.exe install StyleCopPlus.MSBuild -Version 4.7.49.5 -ExcludeVersion
	cp StyleCopPlus.MSBuild/tools/StyleCopPlus.dll .
	rmdir StyleCopPlus.MSBuild -Recurse
}

if (!(Test-Path "StyleCop.dll"))
{
	echo "Fetching StyleCop files from NuGet."
	./nuget.exe install StyleCop.MSBuild -Version 4.7.49.0 -ExcludeVersion
	cp StyleCop.MSBuild/tools/StyleCop*.dll .
	rmdir StyleCop.MSBuild -Recurse
}

if (!(Test-Path "ICSharpCode.SharpZipLib.dll"))
{
	echo "Fetching ICSharpCode.SharpZipLib from NuGet."
	./nuget.exe install SharpZipLib -Version 0.86.0 -ExcludeVersion
	cp SharpZipLib/lib/20/ICSharpCode.SharpZipLib.dll .
	rmdir SharpZipLib -Recurse
}

if (!(Test-Path "MaxMind.GeoIP2.dll"))
{
	echo "Fetching MaxMind.GeoIP2 from NuGet."
	./nuget.exe install MaxMind.GeoIP2 -Version 2.1.0 -ExcludeVersion
	cp MaxMind.Db/lib/net40/MaxMind.Db.* .
	rmdir MaxMind.Db -Recurse
	cp MaxMind.GeoIP2/lib/net40/MaxMind.GeoIP2* .
	rmdir MaxMind.GeoIP2 -Recurse
	cp Newtonsoft.Json/lib/net40/Newtonsoft.Json* .
	rmdir Newtonsoft.Json -Recurse
	cp RestSharp/lib/net4-client/RestSharp* .
	rmdir RestSharp -Recurse
}

if (!(Test-Path "SharpFont.dll"))
{
	echo "Fetching SharpFont from NuGet."
	./nuget.exe install SharpFont -Version 3.0.0 -ExcludeVersion
	cp SharpFont/lib/net20/SharpFont* .
	cp SharpFont/config/SharpFont.dll.config .
	rmdir SharpFont -Recurse
	rmdir SharpFont.Dependencies -Recurse
}

if (!(Test-Path "nunit.framework.dll"))
{
	echo "Fetching NUnit from NuGet."
	./nuget.exe install NUnit -Version 2.6.4 -ExcludeVersion
	cp NUnit/lib/nunit.framework* .
	rmdir NUnit -Recurse
}

if (!(Test-Path "windows/SDL2.dll"))
{
	echo "Fetching SDL2 from NuGet."
	./nuget.exe install sdl2 -Version 2.0.3 -ExcludeVersion
	cp sdl2.redist/build/native/bin/Win32/dynamic/SDL2.dll ./windows/
	rmdir sdl2 -Recurse
	rmdir sdl2.redist -Recurse
}

if (!(Test-Path "Mono.Nat.dll"))
{
	echo "Fetching Mono.Nat from NuGet."
	./nuget.exe install Mono.Nat -Version 1.2.21 -ExcludeVersion
	cp Mono.Nat/lib/net40/Mono.Nat.dll .
	rmdir Mono.Nat -Recurse
}

if (!(Test-Path "windows/lua51.dll"))
{
	echo "Fetching Lua 5.1 from NuGet."
	./nuget.exe install lua.binaries -Version 5.1.5 -ExcludeVersion
	cp lua.binaries/bin/win32/dll8/lua5.1.dll ./windows/lua51.dll
	rmdir lua.binaries -Recurse
}

if (!(Test-Path "windows/freetype6.dll"))
{
	echo "Fetching FreeType2 from NuGet."
	./nuget.exe install SharpFont.Dependencies -Version 2.5.5.1 -ExcludeVersion
	cp SharpFont.Dependencies/bin/msvc10/x86/freetype6.dll ./windows/freetype6.dll
	rmdir SharpFont.Dependencies -Recurse
}

if (!(Test-Path "windows/soft_oal.dll"))
{
	echo "Fetching OpenAL Soft from NuGet."
	./nuget.exe install OpenAL-Soft -Version 1.16.0 -ExcludeVersion
	cp OpenAL-Soft/bin/Win32/soft_oal.dll windows/soft_oal.dll
	rmdir OpenAL-Soft -Recurse
}

if (!(Test-Path "Moq.dll"))
{
	echo "Fetching Moq from NuGet."
	./nuget.exe install Moq -Version 4.2.1502.0911 -ExcludeVersion
	cp Moq/lib/net40/Moq.dll .
	rmdir Moq -Recurse
}

if (!(Test-Path "FuzzyLogicLibrary.dll"))
{
	echo "Fetching FuzzyLogicLibrary from NuGet."
	./nuget.exe install FuzzyLogicLibrary -Version 1.2.0 -ExcludeVersion
	cp FuzzyLogicLibrary/bin/Release/FuzzyLogicLibrary.dll .
	rmdir FuzzyLogicLibrary -Recurse
}

if (!(Test-Path "SDL2-CS.dll"))
{
	echo "Fetching SDL2 C# from GitHub."
	$target = Join-Path $pwd.ToString() "SDL2-CS.dll"
	(New-Object System.Net.WebClient).DownloadFile("https://github.com/OpenRA/SDL2-CS/releases/download/20140407/SDL2-CS.dll", $target)
}

if (!(Test-Path "Eluant.dll"))
{
	echo "Fetching Eluant from GitHub."
	$target = Join-Path $pwd.ToString() "Eluant.dll"
	(New-Object System.Net.WebClient).DownloadFile("https://github.com/OpenRA/Eluant/releases/download/20140425/Eluant.dll", $target)
}
