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
	./nuget.exe install StyleCopPlus.MSBuild -Version 4.7.49.5
	cp StyleCopPlus.MSBuild.4.7.49.5/tools/StyleCopPlus.dll .
	rmdir StyleCopPlus.MSBuild.4.7.49.5 -Recurse
}

if (!(Test-Path "StyleCop.dll"))
{
	echo "Fetching StyleCop files from NuGet."
	./nuget.exe install StyleCop.MSBuild -Version 4.7.49.0
	cp StyleCop.MSBuild.4.7.49.0/tools/StyleCop*.dll .
	rmdir StyleCop.MSBuild.4.7.49.0 -Recurse
}

if (!(Test-Path "ICSharpCode.SharpZipLib.dll"))
{
	echo "Fetching ICSharpCode.SharpZipLib from NuGet."
	./nuget.exe install SharpZipLib -Version 0.86.0
	cp SharpZipLib.0.86.0/lib/20/ICSharpCode.SharpZipLib.dll .
	rmdir SharpZipLib.0.86.0 -Recurse
}

if (!(Test-Path "MaxMind.GeoIP2.dll"))
{
	echo "Fetching MaxMind.GeoIP2 from NuGet."
	./nuget.exe install MaxMind.GeoIP2 -Version 2.1.0
	cp MaxMind.Db.1.0.0.0/lib/net40/MaxMind.Db.* .
	rmdir MaxMind.Db.1.0.0.0 -Recurse
	cp MaxMind.GeoIP2.2.1.0.0/lib/net40/MaxMind.GeoIP2* .
	rmdir MaxMind.GeoIP2.2.1.0.0 -Recurse
	cp Newtonsoft.Json.6.0.5/lib/net40/Newtonsoft.Json* .
	rmdir Newtonsoft.Json.6.0.5 -Recurse
	cp RestSharp.105.0.0/lib/net4-client/RestSharp* .
	rmdir RestSharp.105.0.0 -Recurse
}

if (!(Test-Path "SharpFont.dll"))
{
	echo "Fetching SharpFont from NuGet."
	./nuget.exe install SharpFont -Version 2.5.0.1
	cp SharpFont.2.5.0.1/lib/net20/SharpFont* .
	cp SharpFont.2.5.0.1/Content/SharpFont.dll.config .
	cp SharpFont.2.5.0.1/Content/freetype6.dll ./windows/
	rmdir SharpFont.2.5.0.1 -Recurse
}

if (!(Test-Path "nunit.framework.dll"))
{
	echo "Fetching NUnit from NuGet."
	./nuget.exe install NUnit -Version 2.6.4
	cp NUnit.2.6.4/lib/nunit.framework* .
	rmdir NUnit.2.6.4 -Recurse
}

if (!(Test-Path "windows/SDL2.dll"))
{
	echo "Fetching SDL2 from NuGet."
	./nuget.exe install sdl2 -Version 2.0.3
	cp sdl2.redist.2.0.3/build/native/bin/Win32/dynamic/SDL2.dll ./windows/
	rmdir sdl2.2.0.3 -Recurse
	rmdir sdl2.redist.2.0.3 -Recurse
}

if (!(Test-Path "Mono.Nat.dll"))
{
	echo "Fetching Mono.Nat from NuGet."
	./nuget.exe install Mono.Nat -Version 1.2.21
	cp Mono.Nat.1.2.21.0/lib/net40/Mono.Nat.dll .
	rmdir Mono.Nat.1.2.21.0 -Recurse
}

if (!(Test-Path "windows/lua51.dll"))
{
	echo "Fetching Lua 5.1 from NuGet."
	./nuget.exe install lua51.redist -Version 5.1.5
	cp lua51.redist.5.1.5/build/native/bin/Win32/v120/Release/lua5.1.dll ./windows/lua51.dll
	rmdir lua51.redist.5.1.5 -Recurse
}

if (!(Test-Path "windows/zlib1.dll"))
{
	echo "Fetching ZLib from NuGet."
	./nuget.exe install zlib.redist -Version 1.2.8.7
	cp zlib.redist.1.2.8.7/build/native/bin/v120/Win32/Release/dynamic/stdcall/zlib.dll windows/zlib1.dll
	rmdir zlib.redist.1.2.8.7 -Recurse
}

if (!(Test-Path "windows/soft_oal.dll"))
{
	echo "Fetching OpenAL Soft from NuGet."
	./nuget.exe install OpenAL-Soft -Version 1.16.0
	cp OpenAL-Soft.1.16.0/bin/Win32/soft_oal.dll windows/soft_oal.dll
	rmdir OpenAL-Soft.1.16.0 -Recurse
}
