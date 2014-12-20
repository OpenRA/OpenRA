if (!(Test-Path "nuget.exe"))
{
	echo "Fetching NuGet."
	Invoke-WebRequest "http://nuget.org/nuget.exe" -OutFile "nuget.exe"
}
if (!(Test-Path "StyleCop.dll"))
{
	echo "Fetching StyleCop files from NuGet."
	./nuget.exe install StyleCop.MSBuild -Version 4.7.49.0
	cp StyleCop.MSBuild.4.7.49.0/tools/StyleCop*.dll .
	rmdir StyleCop.MSBuild.4.7.49.0 -Recurse
}
