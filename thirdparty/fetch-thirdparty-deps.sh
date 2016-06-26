#!/bin/sh

####
# This file must stay /bin/sh and POSIX compliant for BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# Die on any error for Travis CI to automatically retry:
set -e

download_dir="${0%/*}/download"

mkdir -p "${download_dir}"
cd "${download_dir}"

# https://github.com/travis-ci/travis-ci/issues/3940
if [ ! $TRAVIS ] && which nuget >/dev/null 2>&1; then
	get()
	{
		nuget install "$1" -Version "$2" -ExcludeVersion
	}
else
	get()
	{
		../noget.sh "$1" "$2"
	}
fi

if [ ! -f StyleCopPlus.dll ]; then
	echo "Fetching StyleCopPlus from NuGet"
	get StyleCopPlus.MSBuild 4.7.49.5
	cp ./StyleCopPlus.MSBuild/tools/StyleCopPlus.dll .
	rm -rf StyleCopPlus.MSBuild
fi

if [ ! -f StyleCop.dll ]; then
	echo "Fetching StyleCop files from NuGet"
	get StyleCop.MSBuild 4.7.49.0
	cp ./StyleCop.MSBuild/tools/StyleCop*.dll .
	rm -rf StyleCop.MSBuild
fi

if [ ! -f ICSharpCode.SharpZipLib.dll ]; then
	echo "Fetching ICSharpCode.SharpZipLib from NuGet"
	get SharpZipLib 0.86.0
	cp ./SharpZipLib/lib/20/ICSharpCode.SharpZipLib.dll .
	rm -rf SharpZipLib
fi

if [ ! -f MaxMind.GeoIP2.dll ]; then
	echo "Fetching MaxMind.GeoIP2 from NuGet"
	get Newtonsoft.Json 8.0.3
	get MaxMind.Db 2.0.0
	get MaxMind.GeoIP2 2.6.0
	cp ./MaxMind.Db/lib/net45/MaxMind.Db.* .
	rm -rf MaxMind.Db
	cp ./MaxMind.GeoIP2/lib/net45/MaxMind.GeoIP2* .
	rm -rf MaxMind.GeoIP2
	cp ./Newtonsoft.Json/lib/net45/Newtonsoft.Json* .
	rm -rf Newtonsoft.Json
fi

if [ ! -f SharpFont.dll ]; then
	echo "Fetching SharpFont from NuGet"
	get SharpFont 3.1.0
	cp ./SharpFont/lib/net20/SharpFont* .
	cp ./SharpFont/config/SharpFont.dll.config .
	rm -rf SharpFont SharpFont.Dependencies
fi

if [ ! -f nunit.framework.dll ]; then
	echo "Fetching NUnit from NuGet"
	get NUnit 3.0.1
	cp ./NUnit/lib/net40/nunit.framework* .
	rm -rf NUnit
fi

if [ ! -f nunit3-console.exe ]; then
	echo "Fetching NUnit.Console from NuGet"
	get NUnit.Console 3.0.1
	cp -R ./NUnit.Console/tools/* .
	chmod +x nunit3-console.exe
	rm -rf NUnit.Console
fi

if [ ! -f Open.Nat.dll ]; then
	echo "Fetching Open.Nat from NuGet"
	get Open.Nat 2.0.16
	cp ./Open.Nat/lib/net45/Open.Nat.dll .
	rm -rf Open.Nat
fi

if [ ! -f FuzzyLogicLibrary.dll ]; then
	echo "Fetching FuzzyLogicLibrary from NuGet."
	get FuzzyLogicLibrary 1.2.0
	cp ./FuzzyLogicLibrary/bin/Release/FuzzyLogicLibrary.dll .
	rm -rf FuzzyLogicLibrary
fi

if [ ! -f SDL2-CS.dll -o ! -f SDL2-CS.dll.config ]; then
	echo "Fetching SDL2-CS from GitHub."
	curl -s -L -O https://github.com/OpenRA/SDL2-CS/releases/download/20151227/SDL2-CS.dll
	curl -s -L -O https://github.com/OpenRA/SDL2-CS/releases/download/20151227/SDL2-CS.dll.config
fi

if [ ! -f OpenAL-CS.dll -o ! -f OpenAL-CS.dll.config ]; then
	echo "Fetching OpenAL-CS from GitHub."
	curl -s -L -O https://github.com/OpenRA/OpenAL-CS/releases/download/20151227/OpenAL-CS.dll
	curl -s -L -O https://github.com/OpenRA/OpenAL-CS/releases/download/20151227/OpenAL-CS.dll.config
fi

if [ ! -f Eluant.dll ]; then
	echo "Fetching Eluant from GitHub."
	curl -s -L -O https://github.com/OpenRA/Eluant/releases/download/20160124/Eluant.dll
fi

if [ ! -f SmarIrc4net.dll ]; then
	echo "Fetching SmartIrc4net from NuGet."
	get SmartIrc4net 0.4.5.1
	cp ./SmartIrc4net/lib/net40/SmarIrc4net* .
	rm -rf SmartIrc4net
fi
