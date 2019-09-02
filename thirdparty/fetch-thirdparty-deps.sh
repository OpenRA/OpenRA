#!/bin/sh

####
# This file must stay /bin/sh and POSIX compliant for BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# Die on any error for Travis CI to automatically retry:
set -e

download_dir="${0%/*}/download"

mkdir -p "${download_dir}"
cd "${download_dir}" || exit 1

if [ ! -f ICSharpCode.SharpZipLib.dll ]; then
	echo "Fetching ICSharpCode.SharpZipLib from NuGet"
	../noget.sh SharpZipLib 1.1.0
	cp ./SharpZipLib/lib/net45/ICSharpCode.SharpZipLib.dll .
	rm -rf SharpZipLib
fi

if [ ! -f MaxMind.Db.dll ]; then
	echo "Fetching MaxMind.Db from NuGet"
	../noget.sh MaxMind.Db 2.0.0 -IgnoreDependencies
	cp ./MaxMind.Db/lib/net45/MaxMind.Db.* .
	rm -rf MaxMind.Db
fi

if [ ! -f nunit.framework.dll ]; then
	echo "Fetching NUnit from NuGet"
	../noget.sh NUnit 3.0.1
	cp ./NUnit/lib/net40/nunit.framework* .
	rm -rf NUnit
fi

if [ ! -f nunit3-console.exe ]; then
	echo "Fetching NUnit.Console from NuGet"
	../noget.sh NUnit.Console 3.0.1
	cp -R ./NUnit.Console/tools/* .
	chmod +x nunit3-console.exe
	rm -rf NUnit.Console
fi

if [ ! -f Open.Nat.dll ]; then
	echo "Fetching Open.Nat from NuGet"
	../noget.sh Open.Nat 2.1.0
	if [ -d ./Open.NAT ]; then mv Open.NAT Open.Nat; fi
	cp ./Open.Nat/lib/net45/Open.Nat.dll .
	rm -rf Open.Nat
fi

if [ ! -f FuzzyLogicLibrary.dll ]; then
	echo "Fetching FuzzyLogicLibrary from NuGet."
	../noget.sh FuzzyLogicLibrary 1.2.0
	cp ./FuzzyLogicLibrary/bin/Release/FuzzyLogicLibrary.dll .
	rm -rf FuzzyLogicLibrary
fi

if [ ! -f SDL2-CS.dll ] || [ ! -f SDL2-CS.dll.config ]; then
	echo "Fetching SDL2-CS from GitHub."
	if command -v curl >/dev/null 2>&1; then
		curl -s -L -O https://github.com/OpenRA/SDL2-CS/releases/download/20190907/SDL2-CS.dll
		curl -s -L -O https://github.com/OpenRA/SDL2-CS/releases/download/20190907/SDL2-CS.dll.config
	else
		wget -cq https://github.com/OpenRA/SDL2-CS/releases/download/20190907/SDL2-CS.dll
		wget -cq https://github.com/OpenRA/SDL2-CS/releases/download/20190907/SDL2-CS.dll.config
	fi
fi

if [ ! -f OpenAL-CS.dll ] || [ ! -f OpenAL-CS.dll.config ]; then
	echo "Fetching OpenAL-CS from GitHub."
	if command -v curl >/dev/null 2>&1; then
		curl -s -L -O https://github.com/OpenRA/OpenAL-CS/releases/download/20190907/OpenAL-CS.dll
		curl -s -L -O https://github.com/OpenRA/OpenAL-CS/releases/download/20190907/OpenAL-CS.dll.config
	else
		wget -cq https://github.com/OpenRA/OpenAL-CS/releases/download/20190907/OpenAL-CS.dll
		wget -cq https://github.com/OpenRA/OpenAL-CS/releases/download/20190907/OpenAL-CS.dll.config
	fi
fi

if [ ! -f Eluant.dll ]; then
	echo "Fetching Eluant from GitHub."
	if command -v curl >/dev/null 2>&1; then
		curl -s -L -O https://github.com/OpenRA/Eluant/releases/download/20160124/Eluant.dll
	else
		wget -cq https://github.com/OpenRA/Eluant/releases/download/20160124/Eluant.dll
	fi
fi

if [ ! -f rix0rrr.BeaconLib.dll ]; then
	echo "Fetching rix0rrr.BeaconLib from NuGet."
	../noget.sh rix0rrr.BeaconLib 1.0.1
	cp ./rix0rrr.BeaconLib/lib/net40/rix0rrr.BeaconLib.dll .
	rm -rf rix0rrr.BeaconLib
fi
